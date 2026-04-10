# CapFinLoan — High Level Design (HLD)

## Overview

CapFinLoan is a cloud-native, event-driven microservices application for loan onboarding and approval.
The system is composed of 6 independent backend services, an API Gateway, a React frontend, and supporting
infrastructure (RabbitMQ, Redis, SQL Server, Docker, GitHub Actions CI/CD).

---

## System Architecture Diagram

```mermaid
flowchart TB
    subgraph CLIENT["CLIENT LAYER"]
        FE["<b>React Frontend</b><br/>Vite · React Router · Axios<br/>Tailwind CSS · Recharts<br/>Port 5173"]
        FB["<b>FinBot AI Chatbot</b><br/>Ollama · llama3.2<br/>Loan Advisor · Conversation History<br/>Port 11434"]
    end

    subgraph GATEWAY["API GATEWAY LAYER"]
        GW["<b>Ocelot API Gateway</b><br/>Centralized JWT Validation<br/>Rate Limiting · Request Routing<br/>Route Forwarding to Services<br/>Port 5000"]
    end

    subgraph SERVICES["MICROSERVICES LAYER — ASP.NET Core .NET 10"]
        direction LR
        AUTH["<b>AuthService</b><br/>User Registration &amp; Login<br/>JWT Issuance · BCrypt Hashing<br/>OTP · MFA · Password Reset<br/>Port 5001"]
        APP["<b>ApplicationService</b><br/>Full Loan Lifecycle<br/>Status State Machine<br/>Saga Choreography<br/>Port 5002"]
        DOC["<b>DocumentService</b><br/>KYC File Upload<br/>PDF · JPG · PNG (max 5MB)<br/>Admin Verification<br/>Port 5003"]
        ADMIN["<b>AdminService</b><br/>Loan Decisions<br/>Reports &amp; Dashboard<br/>Redis-Cached Endpoints<br/>Port 5004"]
        NOTIF["<b>NotificationService</b><br/>RabbitMQ Consumer<br/>Email via MailKit<br/>Status Change Alerts<br/>Port 5005"]
        PAY["<b>PaymentService</b><br/>Saga Disbursement<br/>LoanApproved Consumer<br/>PaymentProcessed Publisher<br/>Port 5006"]
    end

    subgraph SHARED["SHARED KERNEL — Class Library"]
        SK["ApiResponseDto · PagedResponseDto<br/>Enums: ApplicationStatus · LoanType · DocumentType<br/>Events: LoanStatusChangedEvent · LoanApprovedEvent · PaymentProcessedEvent<br/>Helpers: PaginationHelper · EmiCalculator"]
    end

    subgraph DATA["DATA LAYER — SQL Server 2022 (1 Database per Service)"]
        direction LR
        DA[("CapFinLoan_Auth<br/>auth.Users")]
        DB[("CapFinLoan_Loan<br/>core.LoanApplications<br/>core.StatusHistory")]
        DC[("CapFinLoan_Document<br/>docs.Documents")]
        DD[("CapFinLoan_Admin<br/>admin.Decisions<br/>admin.Reports")]
        DE[("CapFinLoan_Payment<br/>pay.Payments")]
    end

    subgraph INFRA["INFRASTRUCTURE LAYER"]
        direction LR
        MQ["<b>RabbitMQ</b><br/>Event Bus · Port 5672<br/>Exchange: capfinloan.events<br/>Management UI: Port 15672"]
        RC["<b>Redis Cache</b><br/>Port 6379<br/>Dashboard TTL: 5 min<br/>Reports TTL: 10 min"]
        DK["<b>Docker</b><br/>docker-compose<br/>11 containers<br/>Multi-stage builds"]
        CI["<b>GitHub Actions</b><br/>Build → Test → Coverage Gate<br/>90% threshold · Docker Build<br/>Triggered on push to main"]
    end

    FE -->|"All API calls — Bearer JWT"| GW
    FB -. "AI Chat API" .- FE
    GW -->|"/gateway/auth/*"| AUTH
    GW -->|"/gateway/applications/*"| APP
    GW -->|"/gateway/documents/*"| DOC
    GW -->|"/gateway/admin/*"| ADMIN
    GW -->|"/gateway/payments/*"| PAY

    AUTH --- DA
    APP --- DB
    DOC --- DC
    ADMIN --- DD
    PAY --- DE

    APP -->|"① LoanStatusChangedEvent"| MQ
    APP -->|"② LoanApprovedEvent"| MQ
    MQ -->|"③ loan.status.changed queue"| NOTIF
    MQ -->|"④ loan.approved queue"| PAY
    PAY -->|"⑤ PaymentProcessedEvent"| MQ
    MQ -->|"⑥ payment.processed queue"| APP

    ADMIN <-->|"Cache hit / invalidate"| RC

    SERVICES --> SHARED
```

---

## Saga Choreography Flow (Loan Approval → Disbursement → Close)

```mermaid
sequenceDiagram
    participant Admin
    participant AdminService
    participant RabbitMQ
    participant PaymentService
    participant ApplicationService
    participant NotificationService

    Admin->>AdminService: POST /api/admin/applications/{id}/decision (Approved)
    AdminService->>AdminService: Save Decision to admin.Decisions
    AdminService->>ApplicationService: PUT status → Approved (HTTP internal)
    ApplicationService->>ApplicationService: Insert StatusHistory record
    ApplicationService->>RabbitMQ: Publish LoanApprovedEvent (loan.approved)
    ApplicationService->>RabbitMQ: Publish LoanStatusChangedEvent (loan.status.changed)

    RabbitMQ->>PaymentService: Consume LoanApprovedEvent
    PaymentService->>PaymentService: ProcessPaymentAsync() — simulate disbursement
    PaymentService->>PaymentService: Save pay.Payments record (Completed)
    PaymentService->>RabbitMQ: Publish PaymentProcessedEvent (payment.processed)

    RabbitMQ->>ApplicationService: Consume PaymentProcessedEvent
    ApplicationService->>ApplicationService: Success → Status = Closed
    ApplicationService->>ApplicationService: Insert StatusHistory (Approved → Closed)
    ApplicationService->>RabbitMQ: Publish LoanStatusChangedEvent (Closed)

    RabbitMQ->>NotificationService: Consume LoanStatusChangedEvent
    NotificationService->>NotificationService: Build email for each status change
    NotificationService-->>Admin: Email — Loan Approved
    NotificationService-->>Admin: Email — Loan Closed / Disbursed
```

---

## Component Responsibilities

| Component | Technology | Responsibility |
|---|---|---|
| React Frontend | React 18 · Vite · Tailwind · Recharts | Applicant + Admin UI. 4-step loan wizard, document upload, status tracking, admin dashboard with charts |
| FinBot AI | Ollama · llama3.2 | Conversational loan advisor chatbot. Maintains conversation history. Graceful fallback if offline |
| Ocelot Gateway | Ocelot .NET | Single entry point. JWT validation, rate limiting, route forwarding. Separate configs for local vs Docker |
| AuthService | ASP.NET Core .NET 10 | Registration, BCrypt hashing, JWT issuance, OTP generation/validation, password reset |
| ApplicationService | ASP.NET Core .NET 10 | Loan CRUD, strict status state machine, status history on every transition, Saga consumer/publisher |
| DocumentService | ASP.NET Core .NET 10 | Multipart file upload, type/size validation, stored on disk, admin verification workflow |
| AdminService | ASP.NET Core .NET 10 | Approve/reject with sanction terms, EMI calculation, reports, Redis-cached dashboard |
| NotificationService | ASP.NET Core .NET 10 | Stateless RabbitMQ consumer. Builds and sends status-specific emails via MailKit |
| PaymentService | ASP.NET Core .NET 10 | Saga step 2. Processes disbursement, publishes outcome to close or revert the loan |
| SQL Server | SQL Server 2022 | One database per service. EF Core Code-First migrations. Schema isolation (auth/core/docs/admin/pay) |
| RabbitMQ | RabbitMQ 3.x | Direct exchange `capfinloan.events`. Durable queues. Manual ack. Dead-letter on failure |
| Redis | Redis 7 | In-memory cache on AdminService. Dashboard stats (5 min TTL), report data (10 min TTL) |
| Docker | Docker Compose | All 11 services containerized. Health checks on SQL Server, RabbitMQ, Redis |
| GitHub Actions | CI/CD | Build → Test → 90% coverage gate → Docker build on every push to main |

---

## Security Architecture

| Concern | Implementation |
|---|---|
| Password storage | BCrypt hash — never stored plain |
| Authentication | JWT Bearer tokens — HmacSha256, 60 min expiry |
| Authorization | Role-based — `[Authorize]` for Applicant, `[Authorize(Roles="Admin")]` for Admin |
| UserId source | Always from JWT claims — never from request body |
| Gateway enforcement | Ocelot validates JWT centrally — services also validate independently for Swagger testing |
| TC12 | `GET /gateway/admin/*` with Applicant token → 403 Forbidden |
| Secrets | Connection strings in appsettings.json / Docker .env — never hardcoded |

---

## Deployment Architecture (Docker)

```mermaid
flowchart LR
    subgraph DOCKER["docker-compose — 11 Containers"]
        direction TB
        sqlserver["sqlserver\nSQL Server 2022\nPort 1433"]
        rabbitmq["rabbitmq\nRabbitMQ + Management\nPorts 5672, 15672"]
        redis["redis\nRedis 7\nPort 6379"]
        ollama["ollama\nOllama AI\nPort 11434"]
        gateway["gateway\nOcelot\nPort 5000"]
        auth["auth-service\nPort 5001"]
        application["application-service\nPort 5002"]
        document["document-service\nPort 5003"]
        admin["admin-service\nPort 5004"]
        notification["notification-service\nPort 5005"]
        payment["payment-service\nPort 5006"]
    end

    frontend["React Frontend\nnpm run dev\nPort 5173"]
    frontend --> gateway
    gateway --> auth & application & document & admin & payment
    auth & application & document & admin & payment --> sqlserver
    application --> rabbitmq
    rabbitmq --> notification & payment
    admin --> redis
```
