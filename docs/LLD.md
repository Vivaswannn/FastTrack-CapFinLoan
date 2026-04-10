# CapFinLoan — Low Level Design (LLD)

## Overview

This document describes the internal design of each microservice — the layered architecture pattern,
all domain models with fields, service interfaces, repository interfaces, and the loan status state machine.

---

## 1. Layered Architecture Pattern (applied in all 6 services)

```mermaid
flowchart LR
    subgraph HTTP["HTTP Layer"]
        REQ["Incoming HTTP Request\nBearer JWT Token"]
    end

    subgraph CTRL["Controller Layer"]
        C["XxxController\n─────────────────\n✓ Receives HTTP request\n✓ Reads UserId/Email/Role\n  from JWT claims only\n✓ Calls service method\n✓ Returns ApiResponseDto\n✗ Zero business logic"]
    end

    subgraph SVC["Service Layer"]
        S["IXxxService / XxxService\n─────────────────\n✓ ALL business logic here\n✓ Validates business rules\n✓ Calls repository methods\n✓ Throws typed exceptions:\n  KeyNotFoundException → 404\n  ArgumentException → 400\n  UnauthorizedAccess → 401\n  InvalidOperation → 400\n✗ Never uses DbContext"]
    end

    subgraph REPO["Repository Layer"]
        R["IXxxRepository / XxxRepository\n─────────────────\n✓ ONLY layer using DbContext\n✓ Performs CRUD operations\n✓ Returns domain Model objects\n✗ No business logic ever"]
    end

    subgraph DB["Data Layer"]
        D["XxxDbContext\nSQL Server\nEF Core Code-First"]
    end

    subgraph MW["Cross-Cutting Concerns"]
        direction TB
        EM["ExceptionMiddleware\nMaps exceptions → HTTP codes"]
        LOG["Serilog\nStructured logging"]
        VAL["FluentValidation\nRequest DTO validation"]
        JWT["JWT Middleware\nBearer token validation"]
    end

    REQ --> JWT --> VAL --> C --> S --> R --> D
    EM -. "wraps all" .- C
    LOG -. "logs all layers" .- S
```

---

## 2. Domain Model — All Entities

```mermaid
classDiagram
    class User {
        +Guid UserId PK
        +string FullName max100
        +string Email max150 unique
        +string PasswordHash BCrypt
        +string Phone max15
        +string Role Applicant|Admin
        +bool IsActive default true
        +string OtpCode max6 nullable
        +DateTime OtpExpiry nullable
        +DateTime CreatedAt UTC
        +DateTime UpdatedAt nullable
    }

    class LoanApplication {
        +Guid ApplicationId PK
        +Guid UserId cross-service
        +string LoanType enum-as-string
        +decimal LoanAmount 10k-10M
        +int TenureMonths 6-360
        +string Purpose max500
        +string FullName copied-at-submit
        +string Email copied-at-submit
        +string Phone copied-at-submit
        +DateTime DateOfBirth nullable
        +string Address max500
        +string EmployerName
        +string EmploymentType
        +string JobTitle
        +decimal MonthlyIncome
        +int YearsOfExperience
        +string EmployerAddress max500
        +string Status enum-as-string
        +DateTime CreatedAt UTC
        +DateTime UpdatedAt nullable
        +DateTime SubmittedAt nullable
        +DateTime DecidedAt nullable
    }

    class StatusHistory {
        +Guid HistoryId PK
        +Guid ApplicationId FK
        +string FromStatus enum-as-string
        +string ToStatus enum-as-string
        +string Remarks max500
        +string ChangedBy email-or-system
        +DateTime ChangedAt UTC
    }

    class Document {
        +Guid DocumentId PK
        +Guid ApplicationId cross-service
        +Guid UserId cross-service
        +string DocumentType enum-as-string
        +string FileName max255
        +string FilePath max500
        +string StoredFileName max255 GUID-based
        +string FileExtension max10
        +long FileSizeBytes max5MB
        +bool IsVerified default false
        +DateTime UploadedAt UTC
        +DateTime VerifiedAt nullable
        +string VerifiedBy nullable
        +string VerificationRemarks nullable
        +bool IsReplaced default false
    }

    class Decision {
        +Guid DecisionId PK
        +Guid ApplicationId cross-service
        +Guid UserId cross-service
        +string DecisionType Approved|Rejected
        +string Remarks max1000
        +string SanctionTerms max2000
        +decimal LoanAmountApproved nullable
        +decimal InterestRate nullable
        +int TenureMonths nullable
        +decimal MonthlyEmi nullable calculated
        +string DecidedBy admin-email
        +DateTime DecidedAt UTC
    }

    class Report {
        +Guid ReportId PK
        +string ReportType max100
        +DateTime GeneratedAt UTC
        +string GeneratedBy admin-email
        +string Parameters JSON nullable
        +string FilePath nullable
        +int TotalRecords
    }

    class Payment {
        +Guid PaymentId PK
        +Guid ApplicationId cross-service
        +Guid UserId cross-service
        +decimal AmountDisbursed 18-2
        +string Status Pending|Processing|Completed|Failed
        +string ReferenceNumber max100
        +string Message max500
        +DateTime CreatedAt UTC
        +DateTime ProcessedAt nullable
    }

    LoanApplication "1" --> "many" StatusHistory : has history
    LoanApplication ..> Decision : results in
    LoanApplication ..> Document : has documents
    Decision ..> Payment : triggers via RabbitMQ
```

---

## 3. Loan Status State Machine

```mermaid
stateDiagram-v2
    [*] --> Draft : Applicant creates application\nPOST /api/applications

    Draft --> Submitted : Applicant submits\nPOST /api/applications/{id}/submit\nAll required fields validated

    Submitted --> DocsPending : Admin moves to docs pending\nPUT /api/admin/applications/{id}/status

    DocsPending --> DocsVerified : Admin verifies all KYC docs\nPUT /api/admin/documents/{id}/verify

    DocsVerified --> UnderReview : Admin starts review\nPUT /api/admin/applications/{id}/status

    UnderReview --> Approved : Admin approves with sanction terms\nPOST /api/admin/applications/{id}/decision\nTriggers LoanApprovedEvent → RabbitMQ → PaymentService

    UnderReview --> Rejected : Admin rejects with reason\nPOST /api/admin/applications/{id}/decision\nNotification email sent

    Approved --> Closed : PaymentService saga completes\nPaymentProcessedEvent consumed by ApplicationService

    Rejected --> Closed : Admin closes rejected application\nPUT /api/admin/applications/{id}/status

    UnderReview --> UnderReview : PaymentService saga FAILS\nPaymentProcessedEvent Success=false\nReverted to UnderReview for retry

    Closed --> [*]

    note right of Draft : Only Applicant can create/edit
    note right of Submitted : StatusHistory record created\non EVERY transition
    note right of Approved : RabbitMQ saga begins\nLoanApprovedEvent published
```

---

## 4. Service Interfaces (all services)

### AuthService

```mermaid
classDiagram
    class IUserService {
        <<interface>>
        +RegisterAsync(RegisterRequestDto dto) Task~UserDto~
        +LoginAsync(LoginRequestDto dto) Task~LoginResponseDto~
        +GetProfileAsync(Guid userId) Task~UserDto~
        +GetAllUsersAsync(int page, int pageSize) Task~PagedResponseDto~
        +UpdateUserStatusAsync(Guid userId, bool isActive) Task
        +ForgotPasswordAsync(string email) Task
        +ResetPasswordAsync(ResetPasswordDto dto) Task
    }

    class IUserRepository {
        <<interface>>
        +GetByEmailAsync(string email) Task~User~
        +GetByIdAsync(Guid userId) Task~User~
        +CreateAsync(User user) Task~User~
        +GetAllAsync(int page, int pageSize) Task~List~User~~
        +UpdateAsync(User user) Task
        +GetTotalCountAsync() Task~int~
    }

    class IJwtHelper {
        <<interface>>
        +GenerateToken(User user) string
        +ValidateToken(string token) ClaimsPrincipal
    }

    class IMessagePublisher {
        <<interface>>
        +PublishAsync(string exchange, string routingKey, object message) Task
    }

    UserService ..|> IUserService
    UserService --> IUserRepository
    UserService --> IJwtHelper
    UserService --> IMessagePublisher
    UserRepository ..|> IUserRepository
```

### ApplicationService

```mermaid
classDiagram
    class ILoanApplicationService {
        <<interface>>
        +CreateDraftAsync(CreateLoanApplicationDto dto, Guid userId) Task~LoanApplicationDto~
        +UpdateDraftAsync(Guid appId, UpdateLoanApplicationDto dto, Guid userId) Task~LoanApplicationDto~
        +SubmitAsync(Guid appId, Guid userId) Task~LoanApplicationDto~
        +GetMyApplicationsAsync(Guid userId, int page, int size) Task~PagedResponseDto~
        +GetByIdAsync(Guid appId, Guid userId) Task~LoanApplicationDto~
        +GetStatusHistoryAsync(Guid appId) Task~List~StatusHistoryDto~~
        +GetAllApplicationsAsync(int page, int size, string status) Task~PagedResponseDto~
        +UpdateStatusAsync(Guid appId, UpdateStatusDto dto, string changedBy) Task
    }

    class ILoanApplicationRepository {
        <<interface>>
        +CreateAsync(LoanApplication app) Task~LoanApplication~
        +GetByIdAsync(Guid appId) Task~LoanApplication~
        +GetByUserIdAsync(Guid userId, int page, int size) Task~List~LoanApplication~~
        +GetAllAsync(int page, int size, string status) Task~List~LoanApplication~~
        +UpdateAsync(LoanApplication app) Task
        +GetTotalCountAsync(Guid userId) Task~int~
        +GetTotalCountAllAsync(string status) Task~int~
    }

    class IStatusHistoryRepository {
        <<interface>>
        +CreateAsync(StatusHistory history) Task
        +GetByApplicationIdAsync(Guid appId) Task~List~StatusHistory~~
    }

    LoanApplicationService ..|> ILoanApplicationService
    LoanApplicationService --> ILoanApplicationRepository
    LoanApplicationService --> IStatusHistoryRepository
    LoanApplicationService --> IMessagePublisher
    LoanApplicationRepository ..|> ILoanApplicationRepository
    StatusHistoryRepository ..|> IStatusHistoryRepository
```

### DocumentService

```mermaid
classDiagram
    class IDocumentService {
        <<interface>>
        +UploadDocumentAsync(UploadDocumentDto dto, Guid userId) Task~DocumentDto~
        +GetDocumentsByApplicationAsync(Guid appId, Guid userId) Task~List~DocumentDto~~
        +GetDocumentByIdAsync(Guid docId) Task~DocumentDto~
        +GetFilePathAsync(Guid docId) Task~string~
        +VerifyDocumentAsync(Guid docId, VerifyDocumentDto dto, string verifiedBy) Task~DocumentDto~
        +GetAdminDocumentsAsync(Guid appId) Task~List~DocumentDto~~
    }

    class IDocumentRepository {
        <<interface>>
        +CreateAsync(Document doc) Task~Document~
        +GetByApplicationIdAsync(Guid appId) Task~List~Document~~
        +GetByIdAsync(Guid docId) Task~Document~
        +UpdateAsync(Document doc) Task
    }

    class IFileHelper {
        <<interface>>
        +SaveFileAsync(IFormFile file, string uploadPath) Task~FileMetadata~
        +DeleteFile(string filePath) void
        +IsValidExtension(string ext) bool
        +IsValidSize(long bytes) bool
    }

    DocumentService ..|> IDocumentService
    DocumentService --> IDocumentRepository
    DocumentService --> IFileHelper
    DocumentRepository ..|> IDocumentRepository
```

### AdminService

```mermaid
classDiagram
    class IAdminService {
        <<interface>>
        +MakeDecisionAsync(Guid appId, MakeDecisionDto dto, string decidedBy) Task~DecisionDto~
        +GetDecisionByApplicationAsync(Guid appId) Task~DecisionDto~
        +GetDashboardStatsAsync() Task~DashboardStatsDto~
        +GetReportSummaryAsync(string date) Task~ReportSummaryDto~
        +GetMonthlyTrendAsync() Task~List~MonthlyTrendDto~~
    }

    class IDecisionRepository {
        <<interface>>
        +CreateAsync(Decision decision) Task~Decision~
        +GetByApplicationIdAsync(Guid appId) Task~Decision~
    }

    class IReportRepository {
        <<interface>>
        +GetDashboardStatsAsync() Task~DashboardStatsDto~
        +GetMonthlyTrendAsync() Task~List~MonthlyTrendDto~~
        +CreateReportAsync(Report report) Task
    }

    class ICacheService {
        <<interface>>
        +GetAsync(string key) Task~string~
        +SetAsync(string key, string value, TimeSpan ttl) Task
        +RemoveAsync(string key) Task
    }

    AdminService ..|> IAdminService
    AdminService --> IDecisionRepository
    AdminService --> IReportRepository
    AdminService --> ICacheService
    RedisCacheService ..|> ICacheService
```

### PaymentService

```mermaid
classDiagram
    class IPaymentService {
        <<interface>>
        +ProcessPaymentAsync(LoanApprovedEvent evt) Task~PaymentResultDto~
        +GetPaymentByApplicationIdAsync(Guid appId) Task~PaymentDto~
        +GetMyPaymentsAsync(Guid userId) Task~List~PaymentDto~~
    }

    class IPaymentRepository {
        <<interface>>
        +CreateAsync(Payment payment) Task~Payment~
        +UpdateAsync(Payment payment) Task
        +GetByApplicationIdAsync(Guid appId) Task~Payment~
        +GetByUserIdAsync(Guid userId) Task~List~Payment~~
    }

    class LoanApprovedConsumer {
        <<BackgroundService>>
        -IServiceScopeFactory _scopeFactory
        +ExecuteAsync(CancellationToken) Task
        -ConnectAndConsumeAsync(CancellationToken) Task
        -HandleEventAsync(LoanApprovedEvent) Task
    }

    class PaymentEventPublisher {
        -IConnection _connection
        +PublishPaymentProcessedAsync(PaymentProcessedEvent) Task
    }

    PaymentProcessingService ..|> IPaymentService
    PaymentProcessingService --> IPaymentRepository
    LoanApprovedConsumer --> IPaymentService
    LoanApprovedConsumer --> PaymentEventPublisher
    PaymentRepository ..|> IPaymentRepository
```

---

## 5. SharedKernel Contents

```mermaid
classDiagram
    class ApiResponseDto {
        +bool Success
        +string Message
        +T Data
        +List~string~ Errors
        +SuccessResponse(T data, string msg)$ ApiResponseDto
        +FailResponse(string msg, List errors)$ ApiResponseDto
        +FailResponse(string msg)$ ApiResponseDto
    }

    class PagedResponseDto {
        +List~T~ Items
        +int TotalCount
        +int Page
        +int PageSize
        +int TotalPages
        +bool HasPreviousPage
        +bool HasNextPage
    }

    class ApplicationStatus {
        <<enumeration>>
        Draft
        Submitted
        DocsPending
        DocsVerified
        UnderReview
        Approved
        Rejected
        Closed
    }

    class LoanType {
        <<enumeration>>
        Personal
        Home
        Vehicle
        Education
        Business
    }

    class DocumentType {
        <<enumeration>>
        AadhaarCard
        PAN
        Passport
        SalarySlip
        BankStatement
        ITReturn
        UtilityBill
        Other
    }

    class DecisionType {
        <<enumeration>>
        Approved
        Rejected
    }

    class LoanStatusChangedEvent {
        +Guid EventId
        +Guid ApplicationId
        +Guid UserId
        +string Email
        +string ApplicantName
        +string OldStatus
        +string NewStatus
        +string Remarks
        +DateTime Timestamp
    }

    class LoanApprovedEvent {
        +Guid EventId
        +Guid ApplicationId
        +Guid UserId
        +string ApplicantEmail
        +string ApplicantName
        +decimal LoanAmountApproved
        +decimal InterestRate
        +int TenureMonths
        +decimal MonthlyEmi
        +string ApprovedBy
        +DateTime Timestamp
    }

    class PaymentProcessedEvent {
        +Guid EventId
        +Guid PaymentId
        +Guid ApplicationId
        +Guid UserId
        +bool Success
        +decimal AmountDisbursed
        +string Message
        +DateTime Timestamp
    }

    class PaginationHelper {
        <<static>>
        +CreatePagedResponse(List items, int total, int page, int size)$ PagedResponseDto
    }
```

---

## 6. Exception Handling Flow (in all services)

```mermaid
flowchart TD
    REQ["Incoming Request"] --> MW["ExceptionMiddleware\nwraps entire pipeline"]
    MW --> CTRL["Controller"]
    CTRL --> SVC["Service throws exception"]
    
    SVC -->|"KeyNotFoundException"| E1["404 Not Found\nRecord does not exist"]
    SVC -->|"ArgumentException"| E2["400 Bad Request\nInvalid input or state"]
    SVC -->|"UnauthorizedAccessException"| E3["401 Unauthorized\nAccess denied"]
    SVC -->|"InvalidOperationException"| E4["400 Bad Request\nBusiness rule violated\ne.g. invalid status transition"]
    SVC -->|"Any other Exception"| E5["500 Internal Server Error\nStack trace hidden from client"]

    E1 & E2 & E3 & E4 & E5 --> RESP["Response:\n{statusCode: int, message: string}\nLogged by Serilog"]
```

---

## 7. RabbitMQ Event Architecture

| Event | Published By | Queue | Consumed By | Action |
|---|---|---|---|---|
| `LoanStatusChangedEvent` | ApplicationService | `loan.status.changed` | NotificationService | Send status email to applicant |
| `LoanApprovedEvent` | ApplicationService | `loan.approved` | PaymentService | Initiate loan disbursement |
| `PaymentProcessedEvent` | PaymentService | `payment.processed` | ApplicationService | Close loan (success) or revert to UnderReview (failure) |

**Pattern:** Consumer-owns-queue. Publisher declares exchange only. Consumer declares queue + binding + dead-letter args.

**Exchange:** `capfinloan.events` — Direct type, durable.

**Resilience:** BackgroundService consumers auto-reconnect with 10s retry on disconnect. Manual ack/nack. Failed messages → dead-letter queue (no message loss).
