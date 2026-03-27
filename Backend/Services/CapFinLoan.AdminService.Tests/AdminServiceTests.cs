using System.Text;
using CapFinLoan.AdminService.DTOs.Requests;
using CapFinLoan.AdminService.DTOs.Responses;
using CapFinLoan.AdminService.Features.Commands;
using CapFinLoan.AdminService.Features.Queries;
using CapFinLoan.AdminService.Helpers;
using CapFinLoan.AdminService.Messaging;
using CapFinLoan.AdminService.Models;
using CapFinLoan.AdminService.Repositories.Interfaces;
using CapFinLoan.AdminService.Services.Interfaces;
using CapFinLoan.AdminService.Validators;
using CapFinLoan.SharedKernel.Enums;
using CapFinLoan.SharedKernel.Events;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace CapFinLoan.AdminService.Tests
{
    [TestFixture]
    public class AdminServiceTests
    {
        private Mock<IDecisionRepository>          _repoMock        = null!;
        private Mock<IApplicationHttpService>      _httpMock        = null!;
        private Mock<ILogger<Services.AdminService>> _logMock       = null!;
        private Mock<ICacheService>                _cacheMock       = null!;
        private Mock<ILoanApprovedPublisher>        _publisherMock  = null!;
        private Services.AdminService              _service         = null!;

        private readonly Guid   _appId      = Guid.NewGuid();
        private readonly Guid   _adminId    = Guid.NewGuid();
        private const   string  AdminEmail  = "admin@capfinloan.com";
        private const   string  AdminToken  = "test-token";

        [SetUp]
        public void SetUp()
        {
            _repoMock      = new Mock<IDecisionRepository>();
            _httpMock      = new Mock<IApplicationHttpService>();
            _logMock       = new Mock<ILogger<Services.AdminService>>();
            _cacheMock     = new Mock<ICacheService>();
            _publisherMock = new Mock<ILoanApprovedPublisher>();

            // Default: cache miss (return null) so tests always hit service logic
            _cacheMock.Setup(c => c.GetAsync<DashboardStatsDto>(It.IsAny<string>()))
                      .ReturnsAsync((DashboardStatsDto?)null);
            _cacheMock.Setup(c => c.GetAsync<List<MonthlyTrendDto>>(It.IsAny<string>()))
                      .ReturnsAsync((List<MonthlyTrendDto>?)null);
            _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>()))
                      .Returns(Task.CompletedTask);
            _publisherMock.Setup(p => p.PublishLoanApprovedAsync(It.IsAny<LoanApprovedEvent>()))
                          .Returns(Task.CompletedTask);

            _service = new Services.AdminService(
                _repoMock.Object,
                _httpMock.Object,
                _logMock.Object,
                _cacheMock.Object,
                _publisherMock.Object);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static Decision BuildDecision(
            DecisionType type,
            decimal? amount  = null,
            decimal? rate    = null,
            int?    tenure   = null,
            decimal? emi     = null) =>
            new Decision
            {
                DecisionId         = Guid.NewGuid(),
                ApplicationId      = Guid.NewGuid(),
                UserId             = Guid.NewGuid(),
                DecisionType       = type,
                Remarks            = "Test remark",
                LoanAmountApproved = amount,
                InterestRate       = rate,
                TenureMonths       = tenure,
                MonthlyEmi         = emi,
                DecidedBy          = AdminEmail,
                DecidedAt          = DateTime.UtcNow
            };

        // ── TEST 1 — TC10: Approve saves decision with EMI calculated ─────────

        [Test]
        public async Task MakeDecisionAsync_Approve_SavesDecisionWithEmi()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByApplicationIdAsync(_appId))
                     .ReturnsAsync((Decision?)null);

            var dto = new MakeDecisionDto
            {
                DecisionType       = "Approved",
                Remarks            = "Approved",
                SanctionTerms      = "EMI terms",
                LoanAmountApproved = 500000,
                InterestRate       = 10.5m,
                TenureMonths       = 36
            };

            var savedDecision = BuildDecision(
                DecisionType.Approved, 500000, 10.5m, 36, 16213.44m);
            savedDecision.ApplicationId = _appId;

            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Decision>()))
                     .ReturnsAsync(savedDecision);

            _httpMock.Setup(h => h.UpdateApplicationStatusAsync(
                         It.IsAny<Guid>(), It.IsAny<string>(),
                         It.IsAny<string>(), It.IsAny<string>()))
                     .Returns(Task.CompletedTask);

            // Act
            var result = await _service.MakeDecisionAsync(
                _appId, dto, _adminId, AdminEmail, AdminToken);

            // Assert
            result.Should().NotBeNull();
            result.DecisionType.Should().Be("Approved");
            result.MonthlyEmi.Should().NotBeNull().And.BeGreaterThan(0);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Decision>()), Times.Once);
            _httpMock.Verify(h => h.UpdateApplicationStatusAsync(
                _appId, "Approved", It.IsAny<string>(), AdminToken), Times.Once);
        }

        // ── TEST 2 — TC11: Reject saves decision ─────────────────────────────

        [Test]
        public async Task MakeDecisionAsync_Reject_SavesDecisionWithoutEmi()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByApplicationIdAsync(_appId))
                     .ReturnsAsync((Decision?)null);

            var dto = new MakeDecisionDto
            {
                DecisionType = "Rejected",
                Remarks      = "Insufficient income score"
            };

            var savedDecision = BuildDecision(DecisionType.Rejected);
            savedDecision.ApplicationId = _appId;

            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Decision>()))
                     .ReturnsAsync(savedDecision);

            _httpMock.Setup(h => h.UpdateApplicationStatusAsync(
                         It.IsAny<Guid>(), It.IsAny<string>(),
                         It.IsAny<string>(), It.IsAny<string>()))
                     .Returns(Task.CompletedTask);

            // Act
            var result = await _service.MakeDecisionAsync(
                _appId, dto, _adminId, AdminEmail, AdminToken);

            // Assert
            result.DecisionType.Should().Be("Rejected");
            result.MonthlyEmi.Should().BeNull();
            _httpMock.Verify(h => h.UpdateApplicationStatusAsync(
                _appId, "Rejected", It.IsAny<string>(), AdminToken), Times.Once);
        }

        // ── TEST 3 — Duplicate decision throws ───────────────────────────────

        [Test]
        public async Task MakeDecisionAsync_DuplicateDecision_ThrowsInvalidOperation()
        {
            // Arrange — existing decision already in DB
            var existing = BuildDecision(DecisionType.Approved);
            _repoMock.Setup(r => r.GetByApplicationIdAsync(_appId))
                     .ReturnsAsync(existing);

            var dto = new MakeDecisionDto
            {
                DecisionType = "Rejected",
                Remarks      = "Second attempt"
            };

            // Act
            var act = async () => await _service.MakeDecisionAsync(
                _appId, dto, _adminId, AdminEmail, AdminToken);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already been made*");

            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Decision>()), Times.Never);
        }

        // ── TEST 4 — EMI calculation gives correct value ──────────────────────

        [Test]
        public void EmiCalculator_StandardLoan_ReturnsCorrectEmi()
        {
            // Act — 5 lakh at 10.5% for 36 months
            var emi = EmiCalculator.CalculateEmi(500000, 10.5m, 36);

            // Assert
            emi.Should().BeGreaterThan(16000);
            emi.Should().BeLessThan(17000);
            emi.Should().BeGreaterThan(0);
        }

        // ── TEST 5 — EMI calculation with zero interest ───────────────────────

        [Test]
        public void EmiCalculator_ZeroInterest_DividesPrincipalByTenure()
        {
            // Act — 3 lakh at 0% for 12 months = 25000/month
            var emi = EmiCalculator.CalculateEmi(300000, 0, 12);

            // Assert
            emi.Should().Be(25000);
        }

        // ── TEST 6 — Dashboard stats correct rates ────────────────────────────

        [Test]
        public async Task GetDashboardStatsAsync_ReturnsCorrectApprovalRate()
        {
            // Arrange
            _repoMock.Setup(r => r.GetApprovedCountAsync()).ReturnsAsync(8);
            _repoMock.Setup(r => r.GetRejectedCountAsync()).ReturnsAsync(2);
            _repoMock.Setup(r => r.GetTotalCountAsync()).ReturnsAsync(10);
            _repoMock.Setup(r => r.GetTotalApprovedAmountAsync()).ReturnsAsync(4_000_000m);

            // Act
            var result = await _service.GetDashboardStatsAsync();

            // Assert
            result.ApprovedCount.Should().Be(8);
            result.RejectedCount.Should().Be(2);
            result.ApprovalRate.Should().Be(80.0m);
        }

        // ── TEST 7 — Dashboard handles zero decisions (no divide-by-zero) ─────

        [Test]
        public async Task GetDashboardStatsAsync_ZeroDecisions_NoException()
        {
            // Arrange
            _repoMock.Setup(r => r.GetApprovedCountAsync()).ReturnsAsync(0);
            _repoMock.Setup(r => r.GetRejectedCountAsync()).ReturnsAsync(0);
            _repoMock.Setup(r => r.GetTotalCountAsync()).ReturnsAsync(0);
            _repoMock.Setup(r => r.GetTotalApprovedAmountAsync()).ReturnsAsync(0m);

            // Act
            var result = await _service.GetDashboardStatsAsync();

            // Assert
            result.Should().NotBeNull();
            result.ApprovalRate.Should().Be(0);
        }

        // ── TEST 8 — ExportDecisionsToCsvAsync returns valid CSV ──────────────

        [Test]
        public async Task ExportDecisionsToCsvAsync_ReturnsValidCsv()
        {
            // Arrange
            var decisions = new List<Decision>
            {
                BuildDecision(DecisionType.Approved, 500000, 10.5m, 36, 16213.44m),
                BuildDecision(DecisionType.Rejected)
            };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(decisions);

            // Act
            var bytes = await _service.ExportDecisionsToCsvAsync(null, null);

            // Assert
            bytes.Should().NotBeNull();
            bytes.Length.Should().BeGreaterThan(0);

            var csv = Encoding.UTF8.GetString(bytes);
            csv.Should().Contain("DecisionId");
            csv.Should().Contain("Approved");
        }

        // ── TEST 9 — Validator: approval without sanction terms fails ─────────

        [Test]
        public void MakeDecisionValidator_ApprovalWithoutSanctionTerms_FailsValidation()
        {
            var dto = new MakeDecisionDto
            {
                DecisionType       = "Approved",
                SanctionTerms      = string.Empty,
                LoanAmountApproved = 500000,
                InterestRate       = 10.5m,
                TenureMonths       = 36,
                Remarks            = "Approved"
            };

            var validator = new MakeDecisionValidator();
            var result = validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e =>
                e.ErrorMessage.Contains("Sanction terms"));
        }

        // ── TEST 10 — GetDecisionByApplicationAsync returns null when none ────

        [Test]
        public async Task GetDecisionByApplicationAsync_NoDecision_ReturnsNull()
        {
            _repoMock.Setup(r => r.GetByApplicationIdAsync(_appId))
                     .ReturnsAsync((Decision?)null);

            var result = await _service.GetDecisionByApplicationAsync(_appId);

            result.Should().BeNull();
        }

        // ── TEST 11 — GetDecisionByApplicationAsync returns existing ──────────

        [Test]
        public async Task GetDecisionByApplicationAsync_Exists_ReturnsDto()
        {
            var decision = BuildDecision(DecisionType.Approved, 500000, 10.5m, 36, 16213.44m);
            _repoMock.Setup(r => r.GetByApplicationIdAsync(_appId)).ReturnsAsync(decision);

            var result = await _service.GetDecisionByApplicationAsync(_appId);

            result.Should().NotBeNull();
            result!.DecisionType.Should().Be("Approved");
            result.MonthlyEmi.Should().Be(16213.44m);
        }

        // ── TEST 12 — GetMonthlyTrendAsync groups by month ──────────────────

        [Test]
        public async Task GetMonthlyTrendAsync_GroupsByMonthCorrectly()
        {
            var now = DateTime.UtcNow;
            var d1 = BuildDecision(DecisionType.Approved);
            d1.DecidedAt = new DateTime(now.Year, now.Month, 1);
            var d2 = BuildDecision(DecisionType.Rejected);
            d2.DecidedAt = new DateTime(now.Year, now.Month, 15);
            var d3 = BuildDecision(DecisionType.Approved);
            d3.DecidedAt = new DateTime(now.Year, now.Month, 20);

            var decisions = new List<Decision> { d1, d2, d3 };

            _repoMock.Setup(r => r.GetMonthlyDecisionsAsync(6)).ReturnsAsync(decisions);

            var result = await _service.GetMonthlyTrendAsync(6);

            result.Should().HaveCount(1);
            result[0].ApprovedCount.Should().Be(2);
            result[0].RejectedCount.Should().Be(1);
            result[0].TotalDecisions.Should().Be(3);
        }

        // ── TEST 13 — GetMonthlyTrendAsync empty ────────────────────────────

        [Test]
        public async Task GetMonthlyTrendAsync_NoDecisions_ReturnsEmptyList()
        {
            _repoMock.Setup(r => r.GetMonthlyDecisionsAsync(6)).ReturnsAsync(new List<Decision>());

            var result = await _service.GetMonthlyTrendAsync(6);

            result.Should().BeEmpty();
        }

        // ── TEST 14 — ExportDecisionsToCsvAsync with date range ─────────────

        [Test]
        public async Task ExportDecisionsToCsvAsync_WithDateRange_UsesDateFilter()
        {
            var start = new DateTime(2026, 1, 1);
            var end = new DateTime(2026, 3, 31);
            var decisions = new List<Decision> { BuildDecision(DecisionType.Approved) };

            _repoMock.Setup(r => r.GetByDateRangeAsync(start, end)).ReturnsAsync(decisions);

            var bytes = await _service.ExportDecisionsToCsvAsync(start, end);

            bytes.Should().NotBeNull();
            _repoMock.Verify(r => r.GetByDateRangeAsync(start, end), Times.Once);
            _repoMock.Verify(r => r.GetAllAsync(), Times.Never);
        }

        // ── TEST 15 — ExportDecisionsToCsvAsync without date range ──────────

        [Test]
        public async Task ExportDecisionsToCsvAsync_NoDateRange_UsesGetAll()
        {
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Decision>());

            var bytes = await _service.ExportDecisionsToCsvAsync(null, null);

            _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
        }

        // ── TEST 16 — CSV header is correct ─────────────────────────────────

        [Test]
        public async Task ExportDecisionsToCsvAsync_HasCorrectCsvHeader()
        {
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Decision>());

            var bytes = await _service.ExportDecisionsToCsvAsync(null, null);
            var csv = Encoding.UTF8.GetString(bytes);

            csv.Should().StartWith("DecisionId,ApplicationId,DecisionType");
        }

        // ── TEST 17 — GetApplicationQueueAsync returns fallback on null ──────

        [Test]
        public async Task GetApplicationQueueAsync_NullResult_ReturnsFallbackJson()
        {
            _httpMock.Setup(h => h.GetApplicationQueueAsync(1, 10, null, AdminToken))
                     .ReturnsAsync((string?)null);

            var result = await _service.GetApplicationQueueAsync(1, 10, null, AdminToken);

            result.Should().Contain("\"items\":[]");
        }

        // ── TEST 18 — MakeDecisionAsync rejection has no EMI ────────────────

        [Test]
        public async Task MakeDecisionAsync_Rejection_DoesNotCalculateEmi()
        {
            _repoMock.Setup(r => r.GetByApplicationIdAsync(_appId)).ReturnsAsync((Decision?)null);

            Decision? capturedDecision = null;
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Decision>()))
                     .Callback<Decision>(d => capturedDecision = d)
                     .ReturnsAsync((Decision d) => d);

            _httpMock.Setup(h => h.UpdateApplicationStatusAsync(
                         It.IsAny<Guid>(), It.IsAny<string>(),
                         It.IsAny<string>(), It.IsAny<string>()))
                     .Returns(Task.CompletedTask);

            var dto = new MakeDecisionDto
            {
                DecisionType = "Rejected",
                Remarks = "Insufficient income"
            };

            await _service.MakeDecisionAsync(_appId, dto, _adminId, AdminEmail, AdminToken);

            capturedDecision!.MonthlyEmi.Should().BeNull();
        }

        // ── TEST 19 — Validator: interest rate out of range ─────────────────

        [Test]
        public void MakeDecisionValidator_InterestRateOutOfRange_FailsValidation()
        {
            var dto = new MakeDecisionDto
            {
                DecisionType       = "Approved",
                Remarks            = "Approved",
                SanctionTerms      = "Terms here",
                LoanAmountApproved = 500000,
                InterestRate       = 50m, // out of range (max 36)
                TenureMonths       = 36
            };

            var validator = new MakeDecisionValidator();
            var result = validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e =>
                e.ErrorMessage.Contains("Interest rate"));
        }

        // ── TEST 20 — Validator: valid rejection ────────────────────────────

        [Test]
        public void MakeDecisionValidator_ValidRejection_Passes()
        {
            var dto = new MakeDecisionDto
            {
                DecisionType = "Rejected",
                Remarks      = "Insufficient income score — below threshold"
            };

            var validator = new MakeDecisionValidator();
            var result = validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        // ── TEST 21 — Validator: rejection short remarks ────────────────────

        [Test]
        public void MakeDecisionValidator_RejectionShortRemarks_Fails()
        {
            var dto = new MakeDecisionDto
            {
                DecisionType = "Rejected",
                Remarks      = "No" // too short
            };

            var validator = new MakeDecisionValidator();
            var result = validator.Validate(dto);

            result.IsValid.Should().BeFalse();
        }

        // ── TEST 22 — Validator: valid approval ─────────────────────────────

        [Test]
        public void MakeDecisionValidator_ValidApproval_Passes()
        {
            var dto = new MakeDecisionDto
            {
                DecisionType       = "Approved",
                Remarks            = "All checks cleared",
                SanctionTerms      = "Standard terms apply",
                LoanAmountApproved = 500000,
                InterestRate       = 10.5m,
                TenureMonths       = 36
            };

            var validator = new MakeDecisionValidator();
            var result = validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        // ── TEST 23 — EMI calculation for various inputs ────────────────────

        [TestCase(100000, 12.0, 12, 8884.88)]
        [TestCase(500000, 10.5, 36, 16252.53)]
        [TestCase(1000000, 8.5, 60, 20516.78)]
        public void EmiCalculator_VariousInputs_ReturnsReasonableValues(
            decimal principal, decimal rate, int months, decimal expectedApprox)
        {
            var emi = EmiCalculator.CalculateEmi(principal, rate, months);

            emi.Should().BeGreaterThan(0);
            emi.Should().BeApproximately(expectedApprox, 200m);
        }

        // ── TEST 24 — Dashboard average loan amount calculation ─────────────

        [Test]
        public async Task GetDashboardStatsAsync_CalculatesAverageLoanAmount()
        {
            _repoMock.Setup(r => r.GetApprovedCountAsync()).ReturnsAsync(4);
            _repoMock.Setup(r => r.GetRejectedCountAsync()).ReturnsAsync(1);
            _repoMock.Setup(r => r.GetTotalCountAsync()).ReturnsAsync(5);
            _repoMock.Setup(r => r.GetTotalApprovedAmountAsync()).ReturnsAsync(2_000_000m);

            var result = await _service.GetDashboardStatsAsync();

            result.AverageLoanAmount.Should().Be(500_000m);
            result.TotalLoanAmountApproved.Should().Be(2_000_000m);
        }

        // ── TEST 25 — Validator: approval missing amount fails ──────────────

        [Test]
        public void MakeDecisionValidator_ApprovalMissingAmount_Fails()
        {
            var dto = new MakeDecisionDto
            {
                DecisionType       = "Approved",
                Remarks            = "Approved",
                SanctionTerms      = "Terms",
                LoanAmountApproved = null,
                InterestRate       = 10.5m,
                TenureMonths       = 36
            };

            var validator = new MakeDecisionValidator();
            var result = validator.Validate(dto);

            result.IsValid.Should().BeFalse();
        }

        // ── TEST 26 — Saga: LoanApprovedEvent published on Approval ──────────

        [Test]
        public async Task MakeDecisionAsync_Approval_PublishesLoanApprovedEvent()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByApplicationIdAsync(_appId))
                     .ReturnsAsync((Decision?)null);

            var dto = new MakeDecisionDto
            {
                DecisionType       = "Approved",
                Remarks            = "Approved",
                SanctionTerms      = "Standard terms",
                LoanAmountApproved = 500000,
                InterestRate       = 10.5m,
                TenureMonths       = 36
            };

            var saved = BuildDecision(DecisionType.Approved, 500000, 10.5m, 36, 16213m);
            saved.ApplicationId = _appId;

            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Decision>()))
                     .ReturnsAsync(saved);
            _httpMock.Setup(h => h.UpdateApplicationStatusAsync(
                         It.IsAny<Guid>(), It.IsAny<string>(),
                         It.IsAny<string>(), It.IsAny<string>()))
                     .Returns(Task.CompletedTask);

            // Act
            await _service.MakeDecisionAsync(_appId, dto, _adminId, AdminEmail, AdminToken);

            // Assert — publisher must be called exactly once for an approval
            _publisherMock.Verify(
                p => p.PublishLoanApprovedAsync(It.Is<LoanApprovedEvent>(
                    e => e.ApplicationId == _appId && e.LoanAmountApproved == 500000)),
                Times.Once);
        }

        // ── TEST 27 — Saga: LoanApprovedEvent NOT published on Rejection ─────

        [Test]
        public async Task MakeDecisionAsync_Rejection_DoesNotPublishLoanApprovedEvent()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByApplicationIdAsync(_appId))
                     .ReturnsAsync((Decision?)null);

            var dto = new MakeDecisionDto
            {
                DecisionType = "Rejected",
                Remarks      = "Insufficient income score"
            };

            var saved = BuildDecision(DecisionType.Rejected);
            saved.ApplicationId = _appId;

            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Decision>()))
                     .ReturnsAsync(saved);
            _httpMock.Setup(h => h.UpdateApplicationStatusAsync(
                         It.IsAny<Guid>(), It.IsAny<string>(),
                         It.IsAny<string>(), It.IsAny<string>()))
                     .Returns(Task.CompletedTask);

            // Act
            await _service.MakeDecisionAsync(_appId, dto, _adminId, AdminEmail, AdminToken);

            // Assert — publisher must NOT be called for a rejection
            _publisherMock.Verify(
                p => p.PublishLoanApprovedAsync(It.IsAny<LoanApprovedEvent>()),
                Times.Never);
        }

        // ── CQRS / MediatR Handler Tests ──────────────────────────────────────
        // Tests below verify that each MediatR handler correctly delegates
        // to IAdminService, demonstrating the CQRS pattern.
        // IMediator is also declared here to confirm the dependency is available.

        private Mock<IMediator> _mediatorMock = null!;

        // ── TEST 26 — MakeDecisionCommandHandler delegates to IAdminService ──

        [Test]
        public async Task MakeDecisionCommandHandler_Approve_DelegatesToAdminService()
        {
            // Arrange
            _mediatorMock = new Mock<IMediator>();

            var logMock = new Mock<ILogger<MakeDecisionCommandHandler>>();
            var handler = new MakeDecisionCommandHandler(_service, logMock.Object);

            _repoMock.Setup(r => r.GetByApplicationIdAsync(_appId))
                     .ReturnsAsync((Decision?)null);

            var dto = new MakeDecisionDto
            {
                DecisionType       = "Approved",
                Remarks            = "All clear",
                SanctionTerms      = "Standard terms",
                LoanAmountApproved = 300000,
                InterestRate       = 9.5m,
                TenureMonths       = 24
            };

            var savedDecision = BuildDecision(DecisionType.Approved, 300000, 9.5m, 24, 13800m);
            savedDecision.ApplicationId = _appId;

            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Decision>()))
                     .ReturnsAsync(savedDecision);
            _httpMock.Setup(h => h.UpdateApplicationStatusAsync(
                         It.IsAny<Guid>(), It.IsAny<string>(),
                         It.IsAny<string>(), It.IsAny<string>()))
                     .Returns(Task.CompletedTask);

            var command = new MakeDecisionCommand(
                _appId, dto, _adminId, AdminEmail, AdminToken);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.DecisionType.Should().Be("Approved");
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Decision>()), Times.Once);
        }

        // ── TEST 27 — GetDashboardStatsQueryHandler delegates to IAdminService ─

        [Test]
        public async Task GetDashboardStatsQueryHandler_ReturnsDashboardStats()
        {
            // Arrange
            var handler = new GetDashboardStatsQueryHandler(_service);

            _repoMock.Setup(r => r.GetApprovedCountAsync()).ReturnsAsync(5);
            _repoMock.Setup(r => r.GetRejectedCountAsync()).ReturnsAsync(2);
            _repoMock.Setup(r => r.GetTotalCountAsync()).ReturnsAsync(7);
            _repoMock.Setup(r => r.GetTotalApprovedAmountAsync()).ReturnsAsync(3_500_000m);

            // Act
            var result = await handler.Handle(
                new GetDashboardStatsQuery(), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.ApprovedCount.Should().Be(5);
            result.RejectedCount.Should().Be(2);
        }

        // ── TEST 28 — GetDecisionByApplicationQueryHandler delegates correctly ─

        [Test]
        public async Task GetDecisionByApplicationQueryHandler_ExistingDecision_ReturnsDto()
        {
            // Arrange
            var handler = new GetDecisionByApplicationQueryHandler(_service);
            var decision = BuildDecision(DecisionType.Rejected);
            decision.ApplicationId = _appId;

            _repoMock.Setup(r => r.GetByApplicationIdAsync(_appId))
                     .ReturnsAsync(decision);

            // Act
            var result = await handler.Handle(
                new GetDecisionByApplicationQuery(_appId), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.DecisionType.Should().Be("Rejected");
        }

        // ── TEST 29 — GetMonthlyTrendQueryHandler delegates correctly ─────────

        [Test]
        public async Task GetMonthlyTrendQueryHandler_Returns_Grouped_Trend()
        {
            // Arrange
            var handler = new GetMonthlyTrendQueryHandler(_service);
            var now = DateTime.UtcNow;
            var d1 = BuildDecision(DecisionType.Approved);
            d1.DecidedAt = new DateTime(now.Year, now.Month, 10);

            _repoMock.Setup(r => r.GetMonthlyDecisionsAsync(6))
                     .ReturnsAsync(new List<Decision> { d1 });

            // Act
            var result = await handler.Handle(
                new GetMonthlyTrendQuery(6), CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            result[0].ApprovedCount.Should().Be(1);
        }

        // ── TEST 30 — GetApplicationQueueQueryHandler returns fallback on null ─

        [Test]
        public async Task GetApplicationQueueQueryHandler_NullFromHttp_ReturnsFallback()
        {
            // Arrange
            var handler = new GetApplicationQueueQueryHandler(_service);

            _httpMock.Setup(h => h.GetApplicationQueueAsync(1, 10, null, AdminToken))
                     .ReturnsAsync((string?)null);

            // Act
            var result = await handler.Handle(
                new GetApplicationQueueQuery(1, 10, null, AdminToken),
                CancellationToken.None);

            // Assert
            result.Should().Contain("\"items\":[]");
        }

        // ── TEST 31 — ExportDecisionsCsvCommandHandler delegates correctly ─────

        [Test]
        public async Task ExportDecisionsCsvCommandHandler_ReturnsNonEmptyCsv()
        {
            // Arrange
            var handler = new ExportDecisionsCsvCommandHandler(_service);

            _repoMock.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(new List<Decision>
                     {
                         BuildDecision(DecisionType.Approved, 400000, 10m, 36, 12889m)
                     });

            // Act
            var result = await handler.Handle(
                new ExportDecisionsCsvCommand(null, null),
                CancellationToken.None);

            // Assert
            result.Should().NotBeEmpty();
            var csv = Encoding.UTF8.GetString(result);
            csv.Should().Contain("Approved");
        }
    }
}
