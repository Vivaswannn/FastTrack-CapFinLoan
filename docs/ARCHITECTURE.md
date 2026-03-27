# CapFinLoan Architecture Diagrams

This document contains the High-Level Design (HLD) and Low-Level Design (LLD) diagrams for the CapFinLoan system, primarily implemented using Mermaid charts.

## High-Level Design (HLD)

The HLD illustrates the overall microservices architecture, showing how the frontend communicates with the API Gateway, which in turn routes requests to various backend services. RabbitMQ is used for asynchronous event-driven communication.

```mermaid
flowchart TD
    %% Define styles
    classDef client fill:#f9f,stroke:#333,stroke-width:2px;
    classDef gateway fill:#ff9,stroke:#333,stroke-width:2px;
    classDef service fill:#bbf,stroke:#333,stroke-width:2px;
    classDef database fill:#dfd,stroke:#333,stroke-width:2px;
    classDef messagebroker fill:#fbb,stroke:#333,stroke-width:2px;

    Client(["React Frontend App"]):::client
    Gateway{"Ocelot API Gateway"}:::gateway
    
    %% Microservices
    AuthService["Auth Service"]:::service
    AdminService["Admin Service"]:::service
    AppService["Application Service"]:::service
    DocService["Document Service"]:::service
    NotifService["Notification Service"]:::service
    
    %% Databases
    DB_Auth[("Auth DB")]:::database
    DB_App[("Application DB")]:::database
    DB_Doc[("Document DB")]:::database

    %% Message Broker
    RabbitMQ(["RabbitMQ Event Bus"]):::messagebroker

    %% External
    BlobStorage[("Azure Blob Storage")]:::database
    MailTracer["SMTP / SendGrid"]:::client

    %% Communication
    Client -->|REST / HTTPS| Gateway
    Gateway -->|Routes| AuthService
    Gateway -->|Routes| AdminService
    Gateway -->|Routes| AppService
    Gateway -->|Routes| DocService
    
    AuthService --> AuthDB
    AppService --> ApplicationDB
    DocService --> DocumentDB
    AdminService --> Gateway
    
    AuthService --> DB_Auth
    AppService --> DB_App
    DocService --> DB_Doc
    
    %% Async Messaging
    AuthService -.->|Publishes OTP Events| RabbitMQ
    AppService -.->|Publishes Status Events| RabbitMQ
    RabbitMQ -.->|Consumes Events| NotifService
    
    %% Document Storage
    DocService -->|Stores/Retrieves| BlobStorage
    
    %% Notifications
    NotifService -->|Sends Email| MailTracer

```

## Low-Level Design (LLD)

The LLD zooms in on the internal architecture of individual microservices. It highlights the Clean Architecture principles (Onion Architecture), showing layers such as Controllers, Services, Repositories, and the SharedKernel.

```mermaid
classDiagram
    %% Core Domain
    class LoanApplication {
        +Guid ApplicationId
        +Guid UserId
        +decimal LoanAmount
        +int TenureMonths
        +ApplicationStatus Status
        +DateTime CreatedAt
    }
    
    class StatusHistory {
        +Guid HistoryId
        +ApplicationStatus FromStatus
        +ApplicationStatus ToStatus
        +string Remarks
    }

    %% Repository Layer
    class ILoanApplicationRepository {
        <<Interface>>
        +CreateAsync()
        +UpdateAsync()
        +GetByIdAsync()
    }
    
    class LoanApplicationRepository {
        -ApplicationDbContext _context
    }
    ILoanApplicationRepository <|-- LoanApplicationRepository
    LoanApplicationRepository --> LoanApplication

    %% Service Layer
    class ILoanApplicationService {
        <<Interface>>
        +CreateDraftAsync()
        +SubmitAsync()
        +UpdateStatusAsync()
    }
    
    class LoanApplicationService {
        -ILoanApplicationRepository _repo
        -IMessagePublisher _publisher
    }
    ILoanApplicationService <|-- LoanApplicationService
    LoanApplicationService --> ILoanApplicationRepository

    %% Controller Layer
    class LoanApplicationController {
        -ILoanApplicationService _service
        +CreateDraft()
        +Submit()
        +UpdateStatus()
    }
    LoanApplicationController --> ILoanApplicationService
    
    %% Messaging
    class IMessagePublisher {
        <<Interface>>
        +PublishLoanStatusChangedAsync()
    }
    LoanApplicationService --> IMessagePublisher

```

## Sequence Diagram: Multi-Factor Authentication (OTP Flow)

This sequence diagram explains the OTP-based MFA authentication mechanism introduced in Phase 2.

```mermaid
sequenceDiagram
    participant User
    participant Frontend
    participant Gateway
    participant AuthService
    participant AuthDB as Database
    participant RabbitMQ
    participant NotifService as NotificationService
    participant Email as SMTP

    User->>Frontend: Enter Email & Password
    Frontend->>Gateway: POST /gateway/auth/login
    Gateway->>AuthService: POST /auth/login
    
    AuthService->>AuthDB: Validate Credentials
    AuthDB-->>AuthService: User Valid
    
    AuthService->>AuthService: Generate 6-digit OTP
    AuthService->>AuthDB: Store OTP Hash & Expiry
    
    AuthService->>RabbitMQ: Publish OtpRequestedEvent
    AuthService-->>Frontend: {RequiresOtp: true}
    
    RabbitMQ->>NotifService: Consume OtpRequestedEvent
    NotifService->>Email: Send OTP Email
    Email-->>User: Delivers 6-digit Code
    
    User->>Frontend: Enter OTP Code
    Frontend->>Gateway: POST /gateway/auth/verify-otp
    Gateway->>AuthService: POST /auth/verify-otp
    
    AuthService->>AuthDB: Fetch User & Hash
    AuthService->>AuthService: Validate OTP
    AuthService->>AuthDB: Clear OTP fields
    
    AuthService->>AuthService: Generate JWT Token
    AuthService-->>Frontend: JWT Token
    
    Frontend-->>User: Login Success & Redirect
```
