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

        private LoanApprovedEvent BuildApprovedEvent(
            decimal amount = 500000m,
            Guid? applicationId = null,
            Guid? userId = null) =>
            new LoanApprovedEvent
            {
                ApplicationId      = applicationId ?? _applicationId,
                UserId             = userId ?? _userId,
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
            var evt = BuildApprovedEvent();

            var result = await _service.ProcessPaymentAsync(evt);

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
            var evt = BuildApprovedEvent(300000m);

            var result = await _service.ProcessPaymentAsync(evt);

            result.Status.Should().BeOneOf("Completed", "Failed");
            result.ProcessedAt.Should().NotBeNull();
            result.Message.Should().NotBeNullOrEmpty();

            if (result.Status == "Completed")
                result.ReferenceNumber.Should().StartWith("DISB-");
        }

        // ── TEST 3 — GetPaymentsByApplicationAsync returns correct records ──

        [Test]
        public async Task GetPaymentsByApplicationAsync_ReturnsPaymentsForApplication()
        {
            await _service.ProcessPaymentAsync(BuildApprovedEvent(200000m));
            await _service.ProcessPaymentAsync(BuildApprovedEvent(300000m));

            // Payment for a different application — must not appear
            await _service.ProcessPaymentAsync(BuildApprovedEvent(
                100000m, applicationId: Guid.NewGuid(), userId: Guid.NewGuid()));

            var results = await _service.GetPaymentsByApplicationAsync(_applicationId);

            results.Should().HaveCount(2);
            results.Should().OnlyContain(p => p.ApplicationId == _applicationId);
        }

        // ── TEST 4 — GetPaymentByIdAsync returns null for missing ID ─────────

        [Test]
        public async Task GetPaymentByIdAsync_NonExistentId_ReturnsNull()
        {
            var result = await _service.GetPaymentByIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        // ── TEST 5 — GetPaymentByIdAsync finds existing payment ──────────────

        [Test]
        public async Task GetPaymentByIdAsync_ExistingPayment_ReturnsDto()
        {
            var created = await _service.ProcessPaymentAsync(BuildApprovedEvent());

            var result = await _service.GetPaymentByIdAsync(created.PaymentId);

            result.Should().NotBeNull();
            result!.PaymentId.Should().Be(created.PaymentId);
            result.ApplicationId.Should().Be(_applicationId);
        }

        // ── TEST 6 — Payment DTO maps all fields correctly ───────────────────

        [Test]
        public async Task ProcessPaymentAsync_ResponseDto_MapsAllFieldsCorrectly()
        {
            var evt = BuildApprovedEvent(750000m);

            var result = await _service.ProcessPaymentAsync(evt);

            result.ApplicationId.Should().Be(_applicationId);
            result.UserId.Should().Be(_userId);
            result.AmountDisbursed.Should().Be(750000m);
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            result.ProcessedAt.Should().NotBeNull();
        }

        // ── TEST 7 — Completed payment has reference number and success message

        [Test]
        [Repeat(10)]
        public async Task ProcessPaymentAsync_CompletedPayment_HasReferenceAndMessage()
        {
            var evt = BuildApprovedEvent(100000m);
            var result = await _service.ProcessPaymentAsync(evt);

            if (result.Status == "Completed")
            {
                result.ReferenceNumber.Should().NotBeNullOrEmpty();
                result.ReferenceNumber!.Should().StartWith("DISB-");
                result.Message.Should().Contain("disbursed successfully");
            }
        }

        // ── TEST 8 — Failed payment has no reference number ──────────────────

        [Test]
        [Repeat(10)]
        public async Task ProcessPaymentAsync_FailedPayment_HasNoReferenceNumber()
        {
            var evt = BuildApprovedEvent(100000m);
            var result = await _service.ProcessPaymentAsync(evt);

            if (result.Status == "Failed")
            {
                result.ReferenceNumber.Should().BeNull();
                result.Message.Should().NotBeNullOrEmpty();
            }
        }

        // ── TEST 9 — Empty application returns empty list ────────────────────

        [Test]
        public async Task GetPaymentsByApplicationAsync_NoPayments_ReturnsEmptyList()
        {
            var results = await _service.GetPaymentsByApplicationAsync(Guid.NewGuid());

            results.Should().NotBeNull();
            results.Should().BeEmpty();
        }

        // ── TEST 10 — Multiple applications are isolated ─────────────────────

        [Test]
        public async Task GetPaymentsByApplicationAsync_MultipleApps_ReturnsSeparately()
        {
            var appId1 = Guid.NewGuid();
            var appId2 = Guid.NewGuid();

            await _service.ProcessPaymentAsync(BuildApprovedEvent(100000m, applicationId: appId1));
            await _service.ProcessPaymentAsync(BuildApprovedEvent(200000m, applicationId: appId1));
            await _service.ProcessPaymentAsync(BuildApprovedEvent(300000m, applicationId: appId2));

            var results1 = await _service.GetPaymentsByApplicationAsync(appId1);
            var results2 = await _service.GetPaymentsByApplicationAsync(appId2);

            results1.Should().HaveCount(2);
            results2.Should().HaveCount(1);
            results1.Should().OnlyContain(p => p.ApplicationId == appId1);
            results2.Should().OnlyContain(p => p.ApplicationId == appId2);
        }

        // ── TEST 11 — Large loan amount is processed correctly ───────────────

        [Test]
        public async Task ProcessPaymentAsync_MaxLoanAmount_ProcessedSuccessfully()
        {
            var evt = BuildApprovedEvent(amount: 10_000_000m); // max allowed

            var result = await _service.ProcessPaymentAsync(evt);

            result.Should().NotBeNull();
            result.AmountDisbursed.Should().Be(10_000_000m);
            result.Status.Should().BeOneOf("Completed", "Failed");
        }

        // ── TEST 12 — PaymentId is unique for each processed event ───────────

        [Test]
        public async Task ProcessPaymentAsync_EachCall_GeneratesUniquePaymentId()
        {
            var result1 = await _service.ProcessPaymentAsync(BuildApprovedEvent(100000m));
            var result2 = await _service.ProcessPaymentAsync(BuildApprovedEvent(200000m));

            result1.PaymentId.Should().NotBe(result2.PaymentId);
        }

        // ── TEST 13 — CreatedAt is set to UTC time ───────────────────────────

        [Test]
        public async Task ProcessPaymentAsync_CreatedAt_IsUtcTime()
        {
            var before = DateTime.UtcNow;
            var result = await _service.ProcessPaymentAsync(BuildApprovedEvent());
            var after  = DateTime.UtcNow;

            result.CreatedAt.Should().BeOnOrAfter(before);
            result.CreatedAt.Should().BeOnOrBefore(after);
        }

        // ── TEST 14 — UserId is persisted from the event ─────────────────────

        [Test]
        public async Task ProcessPaymentAsync_UserId_PersistedFromEvent()
        {
            var specificUserId = Guid.NewGuid();
            var evt = BuildApprovedEvent(userId: specificUserId);

            var result = await _service.ProcessPaymentAsync(evt);

            result.UserId.Should().Be(specificUserId);

            var dbRecord = await _db.Payments.FindAsync(result.PaymentId);
            dbRecord!.UserId.Should().Be(specificUserId);
        }

        // ── TEST 15 — GetPaymentsByApplicationAsync returns ordered by CreatedAt descending

        [Test]
        public async Task GetPaymentsByApplicationAsync_ReturnsOrderedByCreatedAtDescending()
        {
            await _service.ProcessPaymentAsync(BuildApprovedEvent(100000m));
            await Task.Delay(10); // ensure distinct timestamps
            await _service.ProcessPaymentAsync(BuildApprovedEvent(200000m));

            var results = await _service.GetPaymentsByApplicationAsync(_applicationId);

            results.Should().HaveCount(2);
            results[0].CreatedAt.Should().BeOnOrAfter(results[1].CreatedAt);
        }
    }
}
