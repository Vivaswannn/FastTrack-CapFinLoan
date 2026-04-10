# CapFinLoan — Financial Loan Onboarding System


A production-grade, microservices-based loan onboarding and approval platform built with ASP.NET Core .NET 10, React (Vite), and an event-driven architecture.

---

## Diagrams

| Diagram | Link |
|---|---|
| High Level Design (HLD) | [Docs/HLD.md](Docs/HLD.md) |
| Low Level Design (LLD) | [Docs/LLD.md](Docs/LLD.md) |
| ER Diagram | [Docs/ERDiagram.png](Docs/ERDiagram.png) |
| Use Case Diagram | [Docs/UseCaseDiagram.png](Docs/UseCaseDiagram.png) |
| Sequence Diagram | [Docs/SequenceDiagram.png](Docs/SequenceDiagram.png) |
| Class Diagram | [Docs/ClassDiagram.png](Docs/ClassDiagram.png) |
| Postman Collection | [Docs/CapFinLoan.postman_collection.json](Docs/CapFinLoan.postman_collection.json) |

---

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Services & Ports](#services--ports)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
  - [Option 1: Docker (Recommended)](#option-1-docker-recommended)
  - [Option 2: Manual Run](#option-2-manual-run)
- [Test Credentials](#test-credentials)
- [API Endpoints](#api-endpoints)
- [Running Tests](#running-tests)
- [Features](#features)
- [Bonus Features](#bonus-features)

---

## Overview

CapFinLoan is a web-based loan onboarding and approval system. Applicants register, apply for loans via a 4-step guided wizard, upload KYC documents, and track application status in real time. Admins verify documents, approve/reject applications with sanction terms, and generate operational reports with charts.

**PRD Test Cases Covered:** TC01 – TC12 (all passing)

---

## Architecture

```
React Frontend (5173)
        │
        ▼
Ocelot API Gateway (5000)
        │
   ┌────┴─────────────────────────────────┐
   │          │           │               │
AuthService  ApplicationService  DocumentService  AdminService
  (5001)       (5002)           (5003)           (5004)
   │              │                                   │
auth.Users    core.LoanApplications             admin.Decisions
              core.StatusHistory                admin.Reports
              docs.Documents
        │
        ▼ RabbitMQ Events
        │
NotificationService (5005)    PaymentService (5006)
   └── Email via MailKit         └── pay.Payments (Saga)
        │
        ▼ Redis Cache
   AdminService Dashboard/Reports (5 min TTL)
```

**Pattern:** Clean Layered Architecture — Controller → Service → Repository → DbContext

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | React 18 + Vite + Tailwind CSS + Recharts |
| API Gateway | Ocelot (port 5000) |
| Backend | ASP.NET Core .NET 10 — Microservices |
| ORM | Entity Framework Core 9 — Code First |
| Database | SQL Server 2022 (separate DB per service) |
| Auth | JWT Bearer + BCrypt + Role-based authorization |
| Messaging | RabbitMQ — event-driven notifications |
| Caching | Redis — dashboard & report caching |
| Validation | FluentValidation on all request DTOs |
| Logging | Serilog — Console + rolling File sink |
| Testing | NUnit + Moq + FluentAssertions (≥90% coverage) |
| Containers | Docker + docker-compose |
| CI/CD | GitHub Actions |
| AI Chatbot | Ollama + llama3.2 (FinBot) |

---

## Project Structure

```
CapFinLoan/
├── Backend/
│   ├── CapFinLoan.slnx
│   ├── Services/
│   │   ├── CapFinLoan.AuthService/          (port 5001)
│   │   ├── CapFinLoan.AuthService.Tests/
│   │   ├── CapFinLoan.ApplicationService/   (port 5002)
│   │   ├── CapFinLoan.ApplicationService.Tests/
│   │   ├── CapFinLoan.DocumentService/      (port 5003)
│   │   ├── CapFinLoan.DocumentService.Tests/
│   │   ├── CapFinLoan.AdminService/         (port 5004)
│   │   ├── CapFinLoan.AdminService.Tests/
│   │   ├── CapFinLoan.NotificationService/  (port 5005)
│   │   └── CapFinLoan.PaymentService/       (port 5006)
│   ├── ApiGateway/
│   │   └── CapFinLoan.Gateway/              (port 5000)
│   └── SharedKernel/
│       └── CapFinLoan.SharedKernel/
├── Frontend/
│   └── capfinloan-frontend/                 (port 5173)
├── Docs/
│   ├── HLD.png
│   ├── UseCaseDiagram.png
│   ├── ClassDiagram.png
│   ├── SequenceDiagram.png
│   ├── ERDiagram.png
│   └── CapFinLoan.postman_collection.json
├── docker-compose.yml
├── .github/workflows/ci.yml
└── README.md
```

---

## Services & Ports

| Service | Port | Description |
|---|---|---|
| Ocelot API Gateway | 5000 | Central entry point, JWT validation, rate limiting |
| AuthService | 5001 | Registration, Login, JWT, OTP/MFA, Password Reset |
| ApplicationService | 5002 | Loan lifecycle — Draft → Submitted → Approved/Rejected |
| DocumentService | 5003 | KYC file upload, verification |
| AdminService | 5004 | Decisions, Reports, Dashboard (Redis cached) |
| NotificationService | 5005 | RabbitMQ consumer → email via MailKit |
| PaymentService | 5006 | Saga disbursement — LoanApprovedEvent consumer |
| React Frontend | 5173 | Full applicant + admin UI |
| RabbitMQ Management | 15672 | guest / guest |
| Redis | 6379 | Dashboard and report caching |
| Ollama (FinBot) | 11434 | llama3.2 AI chatbot |

---

## Prerequisites

### For Docker (Recommended)
- Docker Desktop
- Git

### For Manual Run
- .NET 10 SDK
- Node.js 20+
- SQL Server 2022 (or Docker)
- RabbitMQ (or Docker)
- Redis (or Docker)
- Ollama (optional — for FinBot AI chatbot)

---

## Getting Started

### Option 1: Docker (Recommended)

```bash
# Clone the repository
git clone <repo-url>
cd CapFinLoan

# Start all services
docker-compose up -d

# Apply EF migrations (first run only)
docker-compose exec authservice dotnet ef database update
docker-compose exec applicationservice dotnet ef database update
docker-compose exec documentservice dotnet ef database update
docker-compose exec adminservice dotnet ef database update
docker-compose exec paymentservice dotnet ef database update

# Frontend
cd CapFinLoan/Frontend/capfinloan-frontend
npm install && npm run dev
```

Access at: http://localhost:5173

---

### Option 2: Manual Run

#### 1. Database Setup

Ensure SQL Server is running at `localhost,1433` with:
- User: `sa`
- Password: `Admin@1234`

#### 2. Run Migrations

```bash
cd CapFinLoan/Backend

dotnet ef database update --project Services/CapFinLoan.AuthService
dotnet ef database update --project Services/CapFinLoan.ApplicationService
dotnet ef database update --project Services/CapFinLoan.DocumentService
dotnet ef database update --project Services/CapFinLoan.AdminService
dotnet ef database update --project Services/CapFinLoan.PaymentService
```

#### 3. Start Backend Services

Open 7 terminals and run each:

```bash
# Terminal 1 — Gateway
cd Backend/ApiGateway/CapFinLoan.Gateway
dotnet run

# Terminal 2 — AuthService
cd Backend/Services/CapFinLoan.AuthService
dotnet run

# Terminal 3 — ApplicationService
cd Backend/Services/CapFinLoan.ApplicationService
dotnet run

# Terminal 4 — DocumentService
cd Backend/Services/CapFinLoan.DocumentService
dotnet run

# Terminal 5 — AdminService
cd Backend/Services/CapFinLoan.AdminService
dotnet run

# Terminal 6 — NotificationService
cd Backend/Services/CapFinLoan.NotificationService
dotnet run

# Terminal 7 — PaymentService
cd Backend/Services/CapFinLoan.PaymentService
dotnet run
```

#### 4. Start Frontend

```bash
cd Frontend/capfinloan-frontend
npm install
npm run dev
```

#### 5. (Optional) FinBot AI Chatbot

```bash
winget install Ollama.Ollama
ollama pull llama3.2
ollama serve
```

---

## Test Credentials

| Role | Email | Password |
|---|---|---|
| Admin | admin@capfinloan.com | Admin@1234 |
| Applicant | Register a new account at /auth/register | — |

---

## API Endpoints

All endpoints are accessed via the gateway at `http://localhost:5000`.

### Auth (`/gateway/auth/`)
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/gateway/auth/register` | Public | Register applicant |
| POST | `/gateway/auth/login` | Public | Login, returns JWT |
| POST | `/gateway/auth/forgot-password` | Public | Send OTP to email |
| POST | `/gateway/auth/reset-password` | Public | Reset password with OTP |
| GET | `/gateway/auth/profile` | Applicant | Own profile |
| GET | `/gateway/auth/users` | Admin | All users paginated |
| PUT | `/gateway/auth/users/{id}/status` | Admin | Activate/deactivate user |

### Applications (`/gateway/applications/`)
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/gateway/applications` | Applicant | Create draft |
| PUT | `/gateway/applications/{id}` | Applicant | Update draft |
| POST | `/gateway/applications/{id}/submit` | Applicant | Submit application |
| GET | `/gateway/applications/my` | Applicant | My applications |
| GET | `/gateway/applications/{id}` | Applicant | Get by ID |
| GET | `/gateway/applications/{id}/status` | Applicant | Status history |
| GET | `/gateway/admin/applications` | Admin | All applications |
| PUT | `/gateway/admin/applications/{id}/status` | Admin | Update status |

### Documents (`/gateway/documents/`)
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/gateway/documents/upload` | Applicant | Upload KYC file |
| GET | `/gateway/documents/{appId}` | Applicant | Docs by application |
| GET | `/gateway/documents/file/{id}` | Applicant | Download file |
| PUT | `/gateway/admin/documents/{id}/verify` | Admin | Verify document |
| GET | `/gateway/admin/documents/{appId}` | Admin | Admin view docs |

### Admin (`/gateway/admin/`)
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/gateway/admin/applications/{id}/decision` | Admin | Approve/Reject |
| GET | `/gateway/admin/decisions/{appId}` | Applicant | Get decision |
| GET | `/gateway/admin/reports/dashboard` | Admin | Dashboard stats |
| GET | `/gateway/admin/reports/summary` | Admin | Report summary |
| GET | `/gateway/admin/reports/monthly` | Admin | Monthly trend |

### Payments (`/gateway/payments/`)
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/gateway/payments/{appId}` | Applicant | Payment by application |
| GET | `/gateway/payments/my` | Applicant | My payments |

---

## Running Tests

```bash
cd CapFinLoan/Backend

# Run all tests
dotnet test CapFinLoan.slnx

# Run with coverage report
dotnet test CapFinLoan.slnx --collect:"XPlat Code Coverage"

# Run specific service tests
dotnet test Services/CapFinLoan.AuthService.Tests
dotnet test Services/CapFinLoan.ApplicationService.Tests
dotnet test Services/CapFinLoan.DocumentService.Tests
dotnet test Services/CapFinLoan.AdminService.Tests
```

**Test Results:** 153+ tests passing, 0 failing across all 4 test projects.
Target coverage: ≥90% on service layer.

---

## Features

### Applicant
- Register and login with JWT authentication
- OTP-based password reset (forgot password flow)
- 4-step guided loan application wizard
- Upload KYC documents (PDF, JPG, PNG — max 5MB)
- Real-time application status tracking with timeline
- View sanction terms and payment disbursement status
- FinBot AI chatbot powered by Ollama/llama3.2

### Admin
- View and manage all loan applications
- Verify/reject KYC documents
- Approve or reject applications with sanction terms
- Dashboard with Recharts charts (Pie, Bar, Line)
- CSV export for reports
- User management (activate/deactivate)

### System
- Ocelot API Gateway with JWT validation + rate limiting
- RabbitMQ event-driven email notifications on status change
- Redis caching on dashboard (5 min) and reports (10 min)
- Saga choreography: Loan Approved → Payment Disbursement → Loan Closed
- Docker + docker-compose for full containerization
- GitHub Actions CI/CD (build → test → coverage → docker build)
- Serilog structured logging in all services

---

## Bonus Features

| Feature | Description |
|---|---|
| RabbitMQ Notifications | Email sent on every loan status change via MailKit |
| Redis Caching | Dashboard stats and monthly reports cached |
| PaymentService | Full saga pattern — loan approved → payment disbursed → loan closed |
| FinBot AI | Ollama llama3.2 chatbot — loan advisor with conversation memory |
| MFA / OTP | OTP-based login MFA and password reset |
| Recharts Dashboard | Pie chart (by status), Bar chart (approval rate), Line chart (monthly trend) |
| Docker | Multi-stage Dockerfiles + full docker-compose with health checks |
| CI/CD | GitHub Actions — build, test, 90% coverage gate, Docker build |

---

## Loan Status Lifecycle

```
Draft → Submitted → DocsPending → DocsVerified → UnderReview → Approved/Rejected → Closed
```

Every transition creates a `StatusHistory` record and triggers a RabbitMQ notification email.

---

## Response Format

All endpoints return a consistent wrapper:

```json
{
  "success": true,
  "message": "Human readable message",
  "data": { },
  "errors": []
}
```

---

*CapFinLoan — 2026*
