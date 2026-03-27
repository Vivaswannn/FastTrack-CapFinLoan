using CapFinLoan.PaymentService.Data;
using CapFinLoan.PaymentService.Services;
using CapFinLoan.SharedKernel.Events;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace CapFinLoan.PaymentService.Tests
{
    /// <summary>
    /// Unit tests for PaymentProcessingService.
    /// Demonstrates Saga event consumption: a LoanApprovedEvent is processed
    /// and a Payment record is created with Completed or Failed status.
    /// Uses EF Core InMemory database to avoid SQL Server dependency.
    /// </summary>
    [TestFixture]
    public class PaymentServiceTests
    {
        private PaymentDbContext _db = null!;
        private Mock<ILogger<PaymentProcessingService>> _logMock = null!;
        private PaymentProcessingService _service = null!;

        private readonly Guid _applicationId = Guid.NewGuid();
        private readonly Guid _userId         = Guid.NewGuid();

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<PaymentDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _db      = new PaymentDbContext(options);
            _logMock = new Mock<ILogger<PaymentProcessingService>>();
            _service = new PaymentProcessingService(_db, _logMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
        }

        // ── Helper ──────────────────────────────────────────────────────────

        private LoanApprovedEvent BuildApprovedEvent(decimal amount = 500000m) =>
            new LoanApprovedEvent
            {
                ApplicationId      = _applicationId,
                UserId             = _userId,
                ApplicantEmail     = "applicant@test.com",
                ApplicantName      = "Test Applicant",
                LoanAmountApproved = amount,
                InterestRate       = 10.5m,
                TenureMonths       = 36,
                MonthlyEmi         = 16213m,
                ApprovedBy         = "admin@capfinloan.com",
                Timestamp          = DateTime.UtcNow
            };

        // ── TEST 1 — Saga consumption creates a Payment record ───────────────

        [Test]
        public async Task ProcessPaymentAsync_CreatesPaymentRecord_InDatabase()
        {
            // Arrange
            var evt = BuildApprovedEvent();

            // Act
            var result = await _service.ProcessPaymentAsync(evt);

            // Assert — Payment record must be persisted
            result.Should().NotBeNull();
            result.PaymentId.Should().NotBe(Guid.Empty);
            result.ApplicationId.Should().Be(_applicationId);
            result.AmountDisbursed.Should().Be(500000m);

            var dbRecord = await _db.Payments.FindAsync(result.PaymentId);
            dbRecord.Should().NotBeNull();
            dbRecord!.ApplicationId.Should().Be(_applicationId);
        }

        // ── TEST 2 — Saga consumption sets status to Completed or Failed ────

        [Test]
        public async Task ProcessPaymentAsync_SetsStatusToCompletedOrFailed()
        {
            // Arrange
            var evt = BuildApprovedEvent(300000m);

            // Act
            var result = await _service.ProcessPaymentAsync(evt);

            // Assert — status must be a terminal state (Completed or Failed)
            result.Status.Should().BeOneOf("Completed", "Failed");
            result.ProcessedAt.Should().NotBeNull();
            result.Message.Should().NotBeNullOrEmpty();

            // Completed payments must have a reference number
            if (result.Status == "Completed")
            {
                result.ReferenceNumber.Should().StartWith("DISB-");
            }
        }

        // ── TEST 3 — GetPaymentsByApplicationAsync returns correct records ──

        [Test]
        public async Task GetPaymentsByApplicationAsync_ReturnsPaymentsForApplication()
        {
            // Arrange — process two events for the same application
            var evt1 = BuildApprovedEvent(200000m);
            var evt2 = BuildApprovedEvent(300000m);

            await _service.ProcessPaymentAsync(evt1);
            await _service.ProcessPaymentAsync(evt2);

            // Also create a payment for a different application
            var otherEvent = new LoanApprovedEvent
            {
                ApplicationId      = Guid.NewGuid(),
                UserId             = Guid.NewGuid(),
                ApplicantEmail     = "other@test.com",
                ApplicantName      = "Other",
                LoanAmountApproved = 100000m,
                InterestRate       = 8m,
                TenureMonths       = 12,
                MonthlyEmi         = 8772m,
                ApprovedBy         = "admin@capfinloan.com"
            };
            await _service.ProcessPaymentAsync(otherEvent);

            // Act
            var results = await _service
                .GetPaymentsByApplicationAsync(_applicationId);

            // Assert — only payments for our application returned
            results.Should().HaveCount(2);
            results.Should().OnlyContain(p => p.ApplicationId == _applicationId);
        }

        // ── TEST 4 — GetPaymentByIdAsync returns null for missing ID ─────────

        [Test]
        public async Task GetPaymentByIdAsync_NonExistentId_ReturnsNull()
        {
            // Act
            var result = await _service.GetPaymentByIdAsync(Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        }

        // ── TEST 5 — GetPaymentByIdAsync finds existing payment ──────────────

        [Test]
        public async Task GetPaymentByIdAsync_ExistingPayment_ReturnsDto()
        {
            // Arrange
            var created = await _service.ProcessPaymentAsync(BuildApprovedEvent());

            // Act
            var result = await _service.GetPaymentByIdAsync(created.PaymentId);

            // Assert
            result.Should().NotBeNull();
            result!.PaymentId.Should().Be(created.PaymentId);
            result.ApplicationId.Should().Be(_applicationId);
        }
    }
}
