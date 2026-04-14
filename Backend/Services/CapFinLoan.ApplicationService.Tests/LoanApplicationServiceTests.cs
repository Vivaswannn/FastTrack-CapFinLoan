using CapFinLoan.ApplicationService.DTOs.Requests;
using CapFinLoan.ApplicationService.DTOs.Responses;
using CapFinLoan.ApplicationService.Hubs;
using CapFinLoan.ApplicationService.Messaging;
using CapFinLoan.ApplicationService.Models;
using CapFinLoan.ApplicationService.Repositories.Interfaces;
using CapFinLoan.ApplicationService.Services;
using CapFinLoan.SharedKernel.Enums;
using CapFinLoan.SharedKernel.Events;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace CapFinLoan.ApplicationService.Tests
{
    /// <summary>
    /// Unit tests for LoanApplicationService.
    /// All repository calls are mocked — no database required.
    /// </summary>
    [TestFixture]
    public class LoanApplicationServiceTests
    {
        private Mock<ILoanApplicationRepository> _repositoryMock = null!;
        private Mock<ILogger<LoanApplicationService>> _loggerMock = null!;
        private Mock<IMessagePublisher> _messagePublisherMock = null!;
        private Mock<IHubContext<LoanStatusHub>> _hubContextMock = null!;
        private LoanApplicationService _service = null!;

        private static readonly Guid TestUserId = Guid.NewGuid();
        private static readonly Guid TestAppId = Guid.NewGuid();
        private const string TestEmail = "applicant@test.com";
        private const string AdminEmail = "admin@capfinloan.com";

        [SetUp]
        public void SetUp()
        {
            _repositoryMock = new Mock<ILoanApplicationRepository>();
            _loggerMock = new Mock<ILogger<LoanApplicationService>>();
            _messagePublisherMock = new Mock<IMessagePublisher>();
            _messagePublisherMock
                .Setup(p => p.PublishLoanStatusChangedAsync(
                    It.IsAny<LoanStatusChangedEvent>()))
                .Returns(Task.CompletedTask);

            // Mock SignalR hub context — tests don't need real WebSocket infrastructure
            _hubContextMock = new Mock<IHubContext<LoanStatusHub>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
            mockClientProxy
                .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
                .Returns(Task.CompletedTask);
            _hubContextMock.Setup(h => h.Clients).Returns(mockClients.Object);

            _service = new LoanApplicationService(
                _repositoryMock.Object,
                _loggerMock.Object,
                _messagePublisherMock.Object,
                _hubContextMock.Object);
        }

        // ─────────────────────────────────────────────────────────
        // TEST 1 — TC04: CreateDraftAsync with valid data
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task CreateDraftAsync_ValidData_ReturnsDraftApplication()
        {
            // Arrange
            CreateLoanApplicationDto dto = BuildValidCreateDto();

            LoanApplication returnedApp = BuildApplication(ApplicationStatus.Draft);

            _repositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<LoanApplication>()))
                .ReturnsAsync(returnedApp);

            _repositoryMock
                .Setup(r => r.AddStatusHistoryAsync(It.IsAny<StatusHistory>()))
                .Returns(Task.CompletedTask);

            // Act
            LoanApplicationResponseDto result = await _service.CreateDraftAsync(TestUserId, dto);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("Draft");
            result.UserId.Should().Be(TestUserId);
            _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<LoanApplication>()), Times.Once);
            _repositoryMock.Verify(r => r.AddStatusHistoryAsync(It.IsAny<StatusHistory>()), Times.Once);
        }

        // ─────────────────────────────────────────────────────────
        // TEST 2 — TC05: SubmitAsync with missing required fields
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task SubmitAsync_MissingEmployerName_ThrowsArgumentException()
        {
            // Arrange — application with empty EmployerName and zero MonthlyIncome
            LoanApplication app = BuildApplication(ApplicationStatus.Draft);
            app.EmployerName = string.Empty;
            app.MonthlyIncome = 0;

            _repositoryMock
                .Setup(r => r.GetByIdAndUserIdAsync(TestAppId, TestUserId))
                .ReturnsAsync(app);

            // Act
            Func<Task> act = async () =>
                await _service.SubmitAsync(TestAppId, TestUserId, TestEmail);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*employment details*");

            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<LoanApplication>()), Times.Never);
        }

        // ─────────────────────────────────────────────────────────
        // TEST 3 — SubmitAsync with valid data
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task SubmitAsync_ValidData_ReturnsSubmittedApplication()
        {
            // Arrange
            LoanApplication app = BuildApplication(ApplicationStatus.Draft);
            app.EmployerName = "TCS";
            app.MonthlyIncome = 80000;

            LoanApplication updatedApp = BuildApplication(ApplicationStatus.Submitted);
            updatedApp.SubmittedAt = DateTime.UtcNow;

            _repositoryMock
                .Setup(r => r.GetByIdAndUserIdAsync(TestAppId, TestUserId))
                .ReturnsAsync(app);

            _repositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<LoanApplication>()))
                .ReturnsAsync((LoanApplication a) => a);

            _repositoryMock
                .Setup(r => r.AddStatusHistoryAsync(It.IsAny<StatusHistory>()))
                .Returns(Task.CompletedTask);

            // Act
            LoanApplicationResponseDto result =
                await _service.SubmitAsync(TestAppId, TestUserId, TestEmail);

            // Assert
            result.Status.Should().Be("Submitted");
            result.SubmittedAt.Should().NotBeNull();
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<LoanApplication>()), Times.Once);
            _repositoryMock.Verify(r => r.AddStatusHistoryAsync(It.IsAny<StatusHistory>()), Times.Once);
        }

        // ─────────────────────────────────────────────────────────
        // TEST 4 — UpdateDraftAsync with non-draft application
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task UpdateDraftAsync_NonDraftStatus_ThrowsInvalidOperationException()
        {
            // Arrange — application already Submitted
            LoanApplication app = BuildApplication(ApplicationStatus.Submitted);

            _repositoryMock
                .Setup(r => r.GetByIdAndUserIdAsync(TestAppId, TestUserId))
                .ReturnsAsync(app);

            // Act
            Func<Task> act = async () =>
                await _service.UpdateDraftAsync(TestAppId, TestUserId, new UpdateLoanApplicationDto());

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Only draft applications can be updated*");

            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<LoanApplication>()), Times.Never);
        }

        // ─────────────────────────────────────────────────────────
        // TEST 5 — UpdateDraftAsync application not found
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task UpdateDraftAsync_ApplicationNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetByIdAndUserIdAsync(TestAppId, TestUserId))
                .ReturnsAsync((LoanApplication?)null);

            // Act
            Func<Task> act = async () =>
                await _service.UpdateDraftAsync(TestAppId, TestUserId, new UpdateLoanApplicationDto());

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // ─────────────────────────────────────────────────────────
        // TEST 6 — UpdateStatusAsync valid transition Submitted→DocsPending
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task UpdateStatusAsync_SubmittedToDocsPending_ReturnsDocsPendingApplication()
        {
            // Arrange
            LoanApplication app = BuildApplication(ApplicationStatus.Submitted);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(TestAppId))
                .ReturnsAsync(app);

            _repositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<LoanApplication>()))
                .ReturnsAsync((LoanApplication a) => a);

            _repositoryMock
                .Setup(r => r.AddStatusHistoryAsync(It.IsAny<StatusHistory>()))
                .Returns(Task.CompletedTask);

            UpdateApplicationStatusDto dto = new UpdateApplicationStatusDto
            {
                NewStatus = ApplicationStatus.DocsPending,
                Remarks = "Documents requested"
            };

            // Act
            LoanApplicationResponseDto result =
                await _service.UpdateStatusAsync(TestAppId, dto, AdminEmail);

            // Assert
            result.Status.Should().Be("DocsPending");
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<LoanApplication>()), Times.Once);
            _repositoryMock.Verify(r => r.AddStatusHistoryAsync(It.IsAny<StatusHistory>()), Times.Once);
        }

        // ─────────────────────────────────────────────────────────
        // TEST 7 — UpdateStatusAsync invalid transition Draft→Approved
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task UpdateStatusAsync_DraftToApproved_ThrowsInvalidOperationException()
        {
            // Arrange
            LoanApplication app = BuildApplication(ApplicationStatus.Draft);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(TestAppId))
                .ReturnsAsync(app);

            UpdateApplicationStatusDto dto = new UpdateApplicationStatusDto
            {
                NewStatus = ApplicationStatus.Approved,
                Remarks = "Trying to skip steps"
            };

            // Act
            Func<Task> act = async () =>
                await _service.UpdateStatusAsync(TestAppId, dto, AdminEmail);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Invalid status transition*");
        }

        // ─────────────────────────────────────────────────────────
        // TEST 8 — UpdateStatusAsync invalid transition Submitted→Approved
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task UpdateStatusAsync_SubmittedToApproved_ThrowsInvalidOperationException()
        {
            // Arrange — skip intermediate steps
            LoanApplication app = BuildApplication(ApplicationStatus.Submitted);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(TestAppId))
                .ReturnsAsync(app);

            UpdateApplicationStatusDto dto = new UpdateApplicationStatusDto
            {
                NewStatus = ApplicationStatus.Approved,
                Remarks = "Skipping to approval"
            };

            // Act
            Func<Task> act = async () =>
                await _service.UpdateStatusAsync(TestAppId, dto, AdminEmail);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        // ─────────────────────────────────────────────────────────
        // TEST 9 — GetByIdAsync applicant cannot see other user's application
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task GetByIdAsync_ApplicantAccessingOtherUsersApp_ThrowsKeyNotFoundException()
        {
            // Arrange — GetByIdAndUserIdAsync returns null (wrong user)
            Guid userId2 = Guid.NewGuid();

            _repositoryMock
                .Setup(r => r.GetByIdAndUserIdAsync(TestAppId, userId2))
                .ReturnsAsync((LoanApplication?)null);

            // Act
            Func<Task> act = async () =>
                await _service.GetByIdAsync(TestAppId, userId2, "Applicant");

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // ─────────────────────────────────────────────────────────
        // TEST 10 — GetByIdAsync admin can see any application
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task GetByIdAsync_AdminRole_UsesGetByIdAsync()
        {
            // Arrange — application belongs to a different user
            Guid differentUserId = Guid.NewGuid();
            LoanApplication app = BuildApplication(ApplicationStatus.Submitted);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(TestAppId))
                .ReturnsAsync(app);

            // Act
            LoanApplicationResponseDto result =
                await _service.GetByIdAsync(TestAppId, differentUserId, "Admin");

            // Assert
            result.Should().NotBeNull();
            // Verify that GetByIdAsync (not GetByIdAndUserIdAsync) was called
            _repositoryMock.Verify(r => r.GetByIdAsync(TestAppId), Times.Once);
            _repositoryMock.Verify(
                r => r.GetByIdAndUserIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
                Times.Never);
        }

        // ─────────────────────────────────────────────────────────
        // TEST 11 — StatusHistory created on every status change
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task UpdateStatusAsync_ValidTransition_CreatesCorrectStatusHistory()
        {
            // Arrange
            LoanApplication app = BuildApplication(ApplicationStatus.Submitted);
            StatusHistory? capturedHistory = null;

            _repositoryMock
                .Setup(r => r.GetByIdAsync(TestAppId))
                .ReturnsAsync(app);

            _repositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<LoanApplication>()))
                .ReturnsAsync((LoanApplication a) => a);

            _repositoryMock
                .Setup(r => r.AddStatusHistoryAsync(It.IsAny<StatusHistory>()))
                .Callback<StatusHistory>(h => capturedHistory = h)
                .Returns(Task.CompletedTask);

            UpdateApplicationStatusDto dto = new UpdateApplicationStatusDto
            {
                NewStatus = ApplicationStatus.DocsPending,
                Remarks = "Docs requested"
            };

            // Act
            await _service.UpdateStatusAsync(TestAppId, dto, AdminEmail);

            // Assert
            capturedHistory.Should().NotBeNull();
            capturedHistory!.FromStatus.Should().Be(ApplicationStatus.Submitted);
            capturedHistory.ToStatus.Should().Be(ApplicationStatus.DocsPending);
            capturedHistory.ChangedBy.Should().Be(AdminEmail);
            capturedHistory.ChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        // ─────────────────────────────────────────────────────────
        // TEST 12 — All valid admin transitions pass (parameterized)
        // ─────────────────────────────────────────────────────────

        [TestCase(ApplicationStatus.Submitted, ApplicationStatus.DocsPending)]
        [TestCase(ApplicationStatus.DocsPending, ApplicationStatus.DocsVerified)]
        [TestCase(ApplicationStatus.DocsVerified, ApplicationStatus.UnderReview)]
        [TestCase(ApplicationStatus.UnderReview, ApplicationStatus.Approved)]
        [TestCase(ApplicationStatus.UnderReview, ApplicationStatus.Rejected)]
        [TestCase(ApplicationStatus.Approved, ApplicationStatus.Closed)]
        [TestCase(ApplicationStatus.Rejected, ApplicationStatus.Closed)]
        public async Task UpdateStatusAsync_AllValidAdminTransitions_Succeed(
            ApplicationStatus from, ApplicationStatus to)
        {
            // Arrange
            LoanApplication app = BuildApplication(from);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(TestAppId))
                .ReturnsAsync(app);

            _repositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<LoanApplication>()))
                .ReturnsAsync((LoanApplication a) => a);

            _repositoryMock
                .Setup(r => r.AddStatusHistoryAsync(It.IsAny<StatusHistory>()))
                .Returns(Task.CompletedTask);

            UpdateApplicationStatusDto dto = new UpdateApplicationStatusDto
            {
                NewStatus = to,
                Remarks = "Valid transition"
            };

            // Act
            Func<Task> act = async () =>
                await _service.UpdateStatusAsync(TestAppId, dto, AdminEmail);

            // Assert — should not throw
            await act.Should().NotThrowAsync();
        }

        // ─────────────────────────────────────────────────────────
        // TEST 13 — SubmitAsync sets SubmittedAt timestamp
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task SubmitAsync_SetsSubmittedAtTimestamp()
        {
            LoanApplication app = BuildApplication(ApplicationStatus.Draft);
            app.EmployerName = "TCS";
            app.MonthlyIncome = 80000;

            _repositoryMock.Setup(r => r.GetByIdAndUserIdAsync(TestAppId, TestUserId)).ReturnsAsync(app);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<LoanApplication>())).ReturnsAsync((LoanApplication a) => a);
            _repositoryMock.Setup(r => r.AddStatusHistoryAsync(It.IsAny<StatusHistory>())).Returns(Task.CompletedTask);

            var result = await _service.SubmitAsync(TestAppId, TestUserId, TestEmail);

            result.SubmittedAt.Should().NotBeNull();
            result.SubmittedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        // ─────────────────────────────────────────────────────────
        // TEST 14 — SubmitAsync on non-draft throws
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task SubmitAsync_AlreadySubmitted_ThrowsInvalidOperationException()
        {
            LoanApplication app = BuildApplication(ApplicationStatus.Submitted);
            _repositoryMock.Setup(r => r.GetByIdAndUserIdAsync(TestAppId, TestUserId)).ReturnsAsync(app);

            Func<Task> act = async () => await _service.SubmitAsync(TestAppId, TestUserId, TestEmail);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Only draft*");
        }

        // ─────────────────────────────────────────────────────────
        // TEST 15 — SubmitAsync application not found
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task SubmitAsync_NotFound_ThrowsKeyNotFoundException()
        {
            _repositoryMock.Setup(r => r.GetByIdAndUserIdAsync(TestAppId, TestUserId))
                .ReturnsAsync((LoanApplication?)null);

            Func<Task> act = async () => await _service.SubmitAsync(TestAppId, TestUserId, TestEmail);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // ─────────────────────────────────────────────────────────
        // TEST 16 — CreateDraftAsync sets correct UserId
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task CreateDraftAsync_SetsCorrectUserIdFromJwt()
        {
            CreateLoanApplicationDto dto = BuildValidCreateDto();
            LoanApplication? capturedApp = null;

            _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<LoanApplication>()))
                .Callback<LoanApplication>(a => capturedApp = a)
                .ReturnsAsync((LoanApplication a) => a);
            _repositoryMock.Setup(r => r.AddStatusHistoryAsync(It.IsAny<StatusHistory>()))
                .Returns(Task.CompletedTask);

            await _service.CreateDraftAsync(TestUserId, dto);

            capturedApp.Should().NotBeNull();
            capturedApp!.UserId.Should().Be(TestUserId);
        }

        // ─────────────────────────────────────────────────────────
        // TEST 17 — UpdateDraftAsync updates only provided fields
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task UpdateDraftAsync_PartialUpdate_OnlyUpdatesProvidedFields()
        {
            LoanApplication app = BuildApplication(ApplicationStatus.Draft);
            _repositoryMock.Setup(r => r.GetByIdAndUserIdAsync(TestAppId, TestUserId)).ReturnsAsync(app);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<LoanApplication>())).ReturnsAsync((LoanApplication a) => a);

            var dto = new UpdateLoanApplicationDto { LoanAmount = 999999 };
            var result = await _service.UpdateDraftAsync(TestAppId, TestUserId, dto);

            result.LoanAmount.Should().Be(999999);
            result.FullName.Should().Be("Test Applicant"); // unchanged
        }

        // ─────────────────────────────────────────────────────────
        // TEST 18 — GetMyApplicationsAsync returns paginated list
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task GetMyApplicationsAsync_ReturnsPagedResponse()
        {
            var apps = new List<LoanApplication> { BuildApplication(ApplicationStatus.Draft) };
            _repositoryMock.Setup(r => r.GetByUserIdAsync(TestUserId, 1, 10)).ReturnsAsync((apps, 1));

            var result = await _service.GetMyApplicationsAsync(TestUserId, 1, 10);

            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.Page.Should().Be(1);
        }

        // ─────────────────────────────────────────────────────────
        // TEST — GetMyApplicationsAsync PageSize=5 returns exactly 5 items
        //        when the mock repository contains more than 5 elements
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task GetMyApplicationsAsync_PageSize5_ReturnsExactly5Items()
        {
            // Arrange — mock repository returns 5 items even though total is 12
            var fiveApps = Enumerable.Range(0, 5)
                .Select(_ => BuildApplication(ApplicationStatus.Draft))
                .ToList();

            _repositoryMock
                .Setup(r => r.GetByUserIdAsync(TestUserId, 1, 5))
                .ReturnsAsync((fiveApps, 12));

            // Act
            var result = await _service.GetMyApplicationsAsync(TestUserId, 1, 5);

            // Assert — exactly 5 items on page; total reflects full dataset
            result.Items.Should().HaveCount(5);
            result.TotalCount.Should().Be(12);
            result.PageSize.Should().Be(5);
            result.Page.Should().Be(1);
            result.TotalPages.Should().Be(3); // Math.Ceiling(12/5) = 3
        }

        // ─────────────────────────────────────────────────────────
        // TEST 19 — GetAllApplicationsAsync with status filter
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task GetAllApplicationsAsync_WithFilter_PassesFilterToRepository()
        {
            _repositoryMock.Setup(r => r.GetAllAsync(1, 10, ApplicationStatus.Submitted))
                .ReturnsAsync((new List<LoanApplication>(), 0));

            await _service.GetAllApplicationsAsync(1, 10, ApplicationStatus.Submitted);

            _repositoryMock.Verify(r => r.GetAllAsync(1, 10, ApplicationStatus.Submitted), Times.Once);
        }

        // ─────────────────────────────────────────────────────────
        // TEST 20 — GetStatusHistoryAsync applicant denied for non-owned app
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task GetStatusHistoryAsync_ApplicantNonOwned_ThrowsKeyNotFound()
        {
            Guid otherUser = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetByIdAndUserIdAsync(TestAppId, otherUser))
                .ReturnsAsync((LoanApplication?)null);

            Func<Task> act = async () =>
                await _service.GetStatusHistoryAsync(TestAppId, otherUser, "Applicant");

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // ─────────────────────────────────────────────────────────
        // TEST 21 — GetStatusHistoryAsync admin can view any
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task GetStatusHistoryAsync_AdminRole_SkipsOwnershipCheck()
        {
            var history = new List<StatusHistory>
            {
                new StatusHistory
                {
                    HistoryId = Guid.NewGuid(),
                    ApplicationId = TestAppId,
                    FromStatus = ApplicationStatus.Draft,
                    ToStatus = ApplicationStatus.Submitted,
                    ChangedBy = TestEmail,
                    ChangedAt = DateTime.UtcNow
                }
            };
            _repositoryMock.Setup(r => r.GetStatusHistoryAsync(TestAppId)).ReturnsAsync(history);

            var result = await _service.GetStatusHistoryAsync(TestAppId, Guid.NewGuid(), "Admin");

            result.Should().HaveCount(1);
            result[0].ToStatus.Should().Be("Submitted");
            _repositoryMock.Verify(r => r.GetByIdAndUserIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        }

        // ─────────────────────────────────────────────────────────
        // TEST 22 — All invalid status transitions throw
        // ─────────────────────────────────────────────────────────

        [TestCase(ApplicationStatus.Draft, ApplicationStatus.DocsPending)]
        [TestCase(ApplicationStatus.Draft, ApplicationStatus.Approved)]
        [TestCase(ApplicationStatus.Submitted, ApplicationStatus.Approved)]
        [TestCase(ApplicationStatus.Submitted, ApplicationStatus.Rejected)]
        [TestCase(ApplicationStatus.DocsPending, ApplicationStatus.Approved)]
        [TestCase(ApplicationStatus.DocsVerified, ApplicationStatus.Approved)]
        [TestCase(ApplicationStatus.Approved, ApplicationStatus.Rejected)]
        [TestCase(ApplicationStatus.Rejected, ApplicationStatus.Approved)]
        [TestCase(ApplicationStatus.Closed, ApplicationStatus.Approved)]
        public async Task UpdateStatusAsync_InvalidTransitions_ThrowsInvalidOperation(
            ApplicationStatus from, ApplicationStatus to)
        {
            LoanApplication app = BuildApplication(from);
            _repositoryMock.Setup(r => r.GetByIdAsync(TestAppId)).ReturnsAsync(app);

            Func<Task> act = async () =>
                await _service.UpdateStatusAsync(TestAppId,
                    new UpdateApplicationStatusDto { NewStatus = to }, AdminEmail);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        // ─────────────────────────────────────────────────────────
        // TEST 23 — UpdateStatusAsync not found
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task UpdateStatusAsync_ApplicationNotFound_ThrowsKeyNotFoundException()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(TestAppId))
                .ReturnsAsync((LoanApplication?)null);

            Func<Task> act = async () =>
                await _service.UpdateStatusAsync(TestAppId,
                    new UpdateApplicationStatusDto { NewStatus = ApplicationStatus.DocsPending },
                    AdminEmail);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // ─────────────────────────────────────────────────────────
        // TEST 24 — CreateDraftAsync creates initial status history
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task CreateDraftAsync_CreatesInitialStatusHistory()
        {
            CreateLoanApplicationDto dto = BuildValidCreateDto();
            StatusHistory? capturedHistory = null;

            _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<LoanApplication>()))
                .ReturnsAsync((LoanApplication a) => a);
            _repositoryMock.Setup(r => r.AddStatusHistoryAsync(It.IsAny<StatusHistory>()))
                .Callback<StatusHistory>(h => capturedHistory = h)
                .Returns(Task.CompletedTask);

            await _service.CreateDraftAsync(TestUserId, dto);

            capturedHistory.Should().NotBeNull();
            capturedHistory!.FromStatus.Should().Be(ApplicationStatus.Draft);
            capturedHistory.ToStatus.Should().Be(ApplicationStatus.Draft);
            capturedHistory.ChangedBy.Should().Be(TestUserId.ToString());
        }

        // ─────────────────────────────────────────────────────────
        // Validator Tests
        // ─────────────────────────────────────────────────────────

        [Test]
        public void CreateLoanApplicationValidator_ValidDto_ShouldPass()
        {
            var dto = BuildValidCreateDto();
            var validator = new Validators.CreateLoanApplicationValidator();
            var result = validator.Validate(dto);
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void CreateLoanApplicationValidator_LoanAmountBelowMin_ShouldFail()
        {
            var dto = BuildValidCreateDto();
            dto.LoanAmount = 5000;
            var validator = new Validators.CreateLoanApplicationValidator();
            var result = validator.Validate(dto);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void CreateLoanApplicationValidator_LoanAmountAboveMax_ShouldFail()
        {
            var dto = BuildValidCreateDto();
            dto.LoanAmount = 20000000;
            var validator = new Validators.CreateLoanApplicationValidator();
            var result = validator.Validate(dto);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void UpdateApplicationStatusValidator_ValidDto_ShouldPass()
        {
            var dto = new UpdateApplicationStatusDto
            {
                NewStatus = ApplicationStatus.DocsPending,
                Remarks = "Requesting docs"
            };
            var validator = new Validators.UpdateApplicationStatusValidator();
            var result = validator.Validate(dto);
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void UpdateLoanApplicationValidator_ValidPartialUpdate_ShouldPass()
        {
            var dto = new UpdateLoanApplicationDto { LoanAmount = 500000 };
            var validator = new Validators.UpdateLoanApplicationValidator();
            var result = validator.Validate(dto);
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void UpdateLoanApplicationValidator_InvalidLoanAmount_ShouldFail()
        {
            var dto = new UpdateLoanApplicationDto { LoanAmount = 5000 };
            var validator = new Validators.UpdateLoanApplicationValidator();
            var result = validator.Validate(dto);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void UpdateLoanApplicationValidator_InvalidTenure_ShouldFail()
        {
            var dto = new UpdateLoanApplicationDto { TenureMonths = 3 };
            var validator = new Validators.UpdateLoanApplicationValidator();
            var result = validator.Validate(dto);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void UpdateLoanApplicationValidator_InvalidEmail_ShouldFail()
        {
            var dto = new UpdateLoanApplicationDto { Email = "notvalid" };
            var validator = new Validators.UpdateLoanApplicationValidator();
            var result = validator.Validate(dto);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void UpdateLoanApplicationValidator_InvalidEmploymentType_ShouldFail()
        {
            var dto = new UpdateLoanApplicationDto { EmploymentType = "Freelance" };
            var validator = new Validators.UpdateLoanApplicationValidator();
            var result = validator.Validate(dto);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void UpdateLoanApplicationValidator_NegativeIncome_ShouldFail()
        {
            var dto = new UpdateLoanApplicationDto { MonthlyIncome = -5000 };
            var validator = new Validators.UpdateLoanApplicationValidator();
            var result = validator.Validate(dto);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void UpdateApplicationStatusValidator_RejectionWithoutRemarks_ShouldFail()
        {
            var dto = new UpdateApplicationStatusDto
            {
                NewStatus = ApplicationStatus.Rejected,
                Remarks = string.Empty
            };
            var validator = new Validators.UpdateApplicationStatusValidator();
            var result = validator.Validate(dto);
            result.IsValid.Should().BeFalse();
        }

        // ─────────────────────────────────────────────────────────
        // TEST — PublishLoanStatusChangedAsync called on UpdateStatusAsync
        // ─────────────────────────────────────────────────────────

        [Test]
        public async Task UpdateStatusAsync_SuccessfulTransition_PublishesEventExactlyOnce()
        {
            // Arrange — application in Submitted status, transition to DocsPending
            var app = BuildApplication(ApplicationStatus.Submitted);
            _repositoryMock
                .Setup(r => r.GetByIdAsync(TestAppId))
                .ReturnsAsync(app);
            _repositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<LoanApplication>()))
                .ReturnsAsync(app);
            _repositoryMock
                .Setup(r => r.AddStatusHistoryAsync(It.IsAny<StatusHistory>()))
                .Returns(Task.CompletedTask);

            var dto = new UpdateApplicationStatusDto
            {
                NewStatus = ApplicationStatus.DocsPending,
                Remarks = "Please upload KYC documents"
            };

            // Act
            await _service.UpdateStatusAsync(TestAppId, dto, AdminEmail);

            // Assert — publisher called exactly once with correct event data
            // Assert — an OutboxMessage should be saved instead of publishing directly
            _repositoryMock.Verify(
                r => r.SaveOutboxMessageAsync(
                    It.Is<OutboxMessage>(m =>
                        m.EventType == "LoanStatusChangedEvent" &&
                        m.Payload.Contains("DocsPending"))),
                Times.Once,
                "SaveOutboxMessageAsync should be called exactly once on status update");
        }

        // ─────────────────────────────────────────────────────────
        // Helper builders
        // ─────────────────────────────────────────────────────────

        private static CreateLoanApplicationDto BuildValidCreateDto()
        {
            return new CreateLoanApplicationDto
            {
                LoanType = LoanType.Personal,
                LoanAmount = 500000,
                TenureMonths = 36,
                Purpose = "Home renovation",
                FullName = "Test Applicant",
                Email = TestEmail,
                Phone = "9876543210",
                EmployerName = "TCS",
                EmploymentType = "Salaried",
                MonthlyIncome = 80000,
                JobTitle = "Software Engineer",
                YearsOfExperience = 3
            };
        }

        private static LoanApplication BuildApplication(ApplicationStatus status)
        {
            return new LoanApplication
            {
                ApplicationId = TestAppId,
                UserId = TestUserId,
                LoanType = LoanType.Personal,
                LoanAmount = 500000,
                TenureMonths = 36,
                Purpose = "Home renovation",
                FullName = "Test Applicant",
                Email = TestEmail,
                Phone = "9876543210",
                EmployerName = "TCS",
                EmploymentType = "Salaried",
                MonthlyIncome = 80000,
                JobTitle = "Software Engineer",
                YearsOfExperience = 3,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
