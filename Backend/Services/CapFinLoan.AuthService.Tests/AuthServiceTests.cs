using CapFinLoan.AuthService.DTOs.Requests;
using CapFinLoan.AuthService.DTOs.Responses;
using CapFinLoan.AuthService.Helpers;
using CapFinLoan.AuthService.Models;
using CapFinLoan.AuthService.Repositories.Interfaces;
using CapFinLoan.AuthService.Services;
using CapFinLoan.AuthService.Validators;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CapFinLoan.AuthService.Messaging;
using Moq;
using NUnit.Framework;

namespace CapFinLoan.AuthService.Tests
{
    /// <summary>
    /// Unit tests for AuthService business logic.
    /// All dependencies are mocked using Moq.
    /// Tests cover TC01, TC02, TC03 from PRD requirements.
    /// Target: ≥90% coverage on AuthService service layer.
    /// </summary>
    [TestFixture]
    public class AuthServiceTests
    {
        private Mock<IUserRepository> _mockUserRepository;
        private Mock<IMessagePublisher> _mockMessagePublisher;
        private Mock<ILogger<Services.AuthService>> _mockLogger;
        private JwtHelper _jwtHelper;
        private Services.AuthService _authService;

        [SetUp]
        public void SetUp()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockMessagePublisher = new Mock<IMessagePublisher>();
            _mockLogger = new Mock<ILogger<Services.AuthService>>();

            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:SecretKey"] =
                        "TestSecretKeyForUnitTestingOnly123456789!!",
                    ["JwtSettings:Issuer"] = "CapFinLoan",
                    ["JwtSettings:Audience"] = "CapFinLoanUsers",
                    ["JwtSettings:ExpiryMinutes"] = "60"
                })
                .Build();

            _jwtHelper = new JwtHelper(config);

            _authService = new Services.AuthService(
                _mockUserRepository.Object,
                _jwtHelper,
                _mockMessagePublisher.Object,
                _mockLogger.Object);
        }

        // ── Helper ──────────────────────────────────────────────

        private static RegisterDto BuildRegisterDto(
            string fullName = "John Doe",
            string email = "john@example.com",
            string phone = "9876543210",
            string password = "Password@123") =>
            new RegisterDto
            {
                FullName = fullName,
                Email = email,
                Phone = phone,
                Password = password,
                ConfirmPassword = password
            };

        private static User BuildUser(
            string email = "john@example.com",
            string role = "Applicant",
            bool isActive = true,
            string password = "Password@123") =>
            new User
            {
                UserId = Guid.NewGuid(),
                FullName = "John Doe",
                Email = email,
                Phone = "9876543210",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow
            };

        // ─────────────────────────────────────────────────
        // TC01: Register with valid details
        // ─────────────────────────────────────────────────

        [Test]
        public async Task RegisterAsync_WithValidDetails_ShouldReturnAuthResponse()
        {
            RegisterDto dto = BuildRegisterDto();
            _mockUserRepository.Setup(r => r.EmailExistsAsync(dto.Email)).ReturnsAsync(false);
            _mockUserRepository.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

            var result = await _authService.RegisterAsync(dto);

            result.Should().NotBeNull();
            result.Token.Should().NotBeNullOrEmpty();
            result.Email.Should().Be(dto.Email.ToLower());
            result.Role.Should().Be("Applicant");
            result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
            _mockUserRepository.Verify(r => r.EmailExistsAsync(dto.Email), Times.Once);
            _mockUserRepository.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Once);
        }

        [Test]
        public async Task RegisterAsync_ShouldHashPassword_NotStorePlainText()
        {
            RegisterDto dto = BuildRegisterDto(email: "jane@example.com", phone: "9876543211");
            _mockUserRepository.Setup(r => r.EmailExistsAsync(dto.Email)).ReturnsAsync(false);

            User? capturedUser = null;
            _mockUserRepository
                .Setup(r => r.CreateAsync(It.IsAny<User>()))
                .Callback<User>(u => capturedUser = u)
                .ReturnsAsync((User u) => u);

            await _authService.RegisterAsync(dto);

            capturedUser.Should().NotBeNull();
            capturedUser!.PasswordHash.Should().NotBe(dto.Password);
            capturedUser.PasswordHash.Should().StartWith("$2a$");
        }

        [Test]
        public async Task RegisterAsync_ShouldTrimFullNameAndEmail()
        {
            RegisterDto dto = BuildRegisterDto(fullName: "  John Doe  ", email: "  JOHN@EXAMPLE.COM  ");
            _mockUserRepository.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

            User? capturedUser = null;
            _mockUserRepository
                .Setup(r => r.CreateAsync(It.IsAny<User>()))
                .Callback<User>(u => capturedUser = u)
                .ReturnsAsync((User u) => u);

            await _authService.RegisterAsync(dto);

            capturedUser!.FullName.Should().Be("John Doe");
            capturedUser.Email.Should().Be("john@example.com");
        }

        [Test]
        public async Task RegisterAsync_ShouldSetRoleAsApplicant()
        {
            RegisterDto dto = BuildRegisterDto();
            _mockUserRepository.Setup(r => r.EmailExistsAsync(dto.Email)).ReturnsAsync(false);
            _mockUserRepository.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

            var result = await _authService.RegisterAsync(dto);

            result.Role.Should().Be("Applicant");
        }

        [Test]
        public async Task RegisterAsync_ShouldSetCreatedAtToUtcNow()
        {
            RegisterDto dto = BuildRegisterDto();
            _mockUserRepository.Setup(r => r.EmailExistsAsync(dto.Email)).ReturnsAsync(false);

            User? capturedUser = null;
            _mockUserRepository
                .Setup(r => r.CreateAsync(It.IsAny<User>()))
                .Callback<User>(u => capturedUser = u)
                .ReturnsAsync((User u) => u);

            await _authService.RegisterAsync(dto);

            capturedUser!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        // ─────────────────────────────────────────────────
        // TC02: Register with duplicate email
        // ─────────────────────────────────────────────────

        [Test]
        public async Task RegisterAsync_WithDuplicateEmail_ShouldThrowArgumentException()
        {
            RegisterDto dto = BuildRegisterDto(email: "existing@example.com");
            _mockUserRepository.Setup(r => r.EmailExistsAsync(dto.Email)).ReturnsAsync(true);

            var ex = Assert.ThrowsAsync<ArgumentException>(
                async () => await _authService.RegisterAsync(dto));

            ex!.Message.Should().Contain("already exists");
            _mockUserRepository.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
        }

        // ─────────────────────────────────────────────────
        // TC03: Login with valid credentials
        // ─────────────────────────────────────────────────

        [Test]
        public async Task LoginAsync_WithValidCredentials_ShouldInitiateOtp()
        {
            User user = BuildUser();
            LoginDto dto = new LoginDto { Email = user.Email, Password = "Password@123" };
            _mockUserRepository.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(user);

            var result = await _authService.LoginAsync(dto);

            result.Should().NotBeNull();
            result.RequiresOtp.Should().BeTrue();
            result.Email.Should().Be(user.Email);
            result.Token.Should().BeNullOrEmpty();
            
            _mockMessagePublisher.Verify(m => m.PublishOtpRequestedAsync(It.IsAny<CapFinLoan.SharedKernel.Events.OtpRequestedEvent>()), Times.Once);
        }

        [Test]
        public async Task LoginAsync_AdminLogin_ShouldInitiateOtp()
        {
            User admin = BuildUser(email: "admin@capfinloan.com", role: "Admin");
            LoginDto dto = new LoginDto { Email = admin.Email, Password = "Password@123" };
            _mockUserRepository.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(admin);

            var result = await _authService.LoginAsync(dto);

            result.RequiresOtp.Should().BeTrue();
            result.Email.Should().Be("admin@capfinloan.com");
        }

        [Test]
        public async Task LoginAsync_WithWrongPassword_ShouldThrowUnauthorized()
        {
            User user = BuildUser();
            LoginDto dto = new LoginDto { Email = user.Email, Password = "WrongPassword@123" };
            _mockUserRepository.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(user);

            var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(
                async () => await _authService.LoginAsync(dto));

            ex!.Message.Should().NotContain("password");
            ex.Message.Should().Contain("Invalid");
        }

        [Test]
        public async Task LoginAsync_WithNonExistentEmail_ShouldThrowUnauthorized()
        {
            LoginDto dto = new LoginDto { Email = "notexists@example.com", Password = "Password@123" };
            _mockUserRepository.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((User?)null);

            var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(
                async () => await _authService.LoginAsync(dto));

            ex!.Message.Should().Contain("Invalid");
        }

        [Test]
        public async Task LoginAsync_WithInactiveAccount_ShouldThrowUnauthorized()
        {
            User inactiveUser = BuildUser(isActive: false);
            LoginDto dto = new LoginDto { Email = inactiveUser.Email, Password = "Password@123" };
            _mockUserRepository.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(inactiveUser);

            var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(
                async () => await _authService.LoginAsync(dto));

            ex!.Message.Should().Contain("deactivated");
        }

        // ─────────────────────────────────────────────────
        // GetProfileAsync
        // ─────────────────────────────────────────────────

        [Test]
        public async Task GetProfileAsync_WithValidId_ShouldReturnUserDto()
        {
            User user = BuildUser();
            _mockUserRepository.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);

            var result = await _authService.GetProfileAsync(user.UserId);

            result.Should().NotBeNull();
            result.UserId.Should().Be(user.UserId);
            result.Email.Should().Be(user.Email);
            result.FullName.Should().Be(user.FullName);
            result.Phone.Should().Be(user.Phone);
            result.Role.Should().Be(user.Role);
            result.IsActive.Should().Be(user.IsActive);
            result.GetType().GetProperty("PasswordHash").Should().BeNull();
        }

        [Test]
        public async Task GetProfileAsync_WithInvalidId_ShouldThrowKeyNotFound()
        {
            Guid nonExistentId = Guid.NewGuid();
            _mockUserRepository.Setup(r => r.GetByIdAsync(nonExistentId)).ReturnsAsync((User?)null);

            Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _authService.GetProfileAsync(nonExistentId));
        }

        [Test]
        public async Task GetProfileAsync_ShouldMapAllFieldsCorrectly()
        {
            var now = DateTime.UtcNow;
            User user = new User
            {
                UserId = Guid.NewGuid(), FullName = "Test User",
                Email = "test@test.com", Phone = "9876543210",
                Role = "Applicant", IsActive = true,
                CreatedAt = now, UpdatedAt = now
            };
            _mockUserRepository.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);

            var result = await _authService.GetProfileAsync(user.UserId);

            result.CreatedAt.Should().Be(now);
            result.UpdatedAt.Should().Be(now);
        }

        // ─────────────────────────────────────────────────
        // UpdateUserStatusAsync
        // ─────────────────────────────────────────────────

        [Test]
        public async Task UpdateUserStatusAsync_DeactivateSystemAdmin_ShouldThrow()
        {
            User adminUser = new User
            {
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Email = "admin@capfinloan.com", Role = "Admin", IsActive = true
            };
            _mockUserRepository.Setup(r => r.GetByIdAsync(adminUser.UserId)).ReturnsAsync(adminUser);

            var ex = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _authService
                    .UpdateUserStatusAsync(adminUser.UserId, new UpdateUserStatusDto { IsActive = false }));

            ex!.Message.Should().Contain("system admin");
        }

        [Test]
        public async Task UpdateUserStatusAsync_ActivateSystemAdmin_ShouldSucceed()
        {
            User adminUser = new User
            {
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Email = "admin@capfinloan.com", Role = "Admin", IsActive = true
            };
            _mockUserRepository.Setup(r => r.GetByIdAsync(adminUser.UserId)).ReturnsAsync(adminUser);
            _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

            var result = await _authService
                .UpdateUserStatusAsync(adminUser.UserId, new UpdateUserStatusDto { IsActive = true });

            result.IsActive.Should().BeTrue();
        }

        [Test]
        public async Task UpdateUserStatusAsync_WithNonExistentUser_ShouldThrowKeyNotFound()
        {
            Guid nonExistentId = Guid.NewGuid();
            _mockUserRepository.Setup(r => r.GetByIdAsync(nonExistentId)).ReturnsAsync((User?)null);

            Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _authService
                    .UpdateUserStatusAsync(nonExistentId, new UpdateUserStatusDto { IsActive = false }));
        }

        [Test]
        public async Task UpdateUserStatusAsync_WithValidUser_ShouldUpdateStatus()
        {
            User user = BuildUser();
            _mockUserRepository.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
            _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

            var result = await _authService
                .UpdateUserStatusAsync(user.UserId, new UpdateUserStatusDto { IsActive = false });

            result.Should().NotBeNull();
            result.IsActive.Should().BeFalse();
            _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        }

        [Test]
        public async Task UpdateUserStatusAsync_ShouldSetUpdatedAt()
        {
            User user = BuildUser();
            _mockUserRepository.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);

            User? capturedUser = null;
            _mockUserRepository
                .Setup(r => r.UpdateAsync(It.IsAny<User>()))
                .Callback<User>(u => capturedUser = u)
                .ReturnsAsync((User u) => u);

            await _authService
                .UpdateUserStatusAsync(user.UserId, new UpdateUserStatusDto { IsActive = false });

            capturedUser!.UpdatedAt.Should().NotBeNull();
            capturedUser.UpdatedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        // ─────────────────────────────────────────────────
        // GetAllUsersAsync
        // ─────────────────────────────────────────────────

        [Test]
        public async Task GetAllUsersAsync_ShouldReturnPagedResponse()
        {
            List<User> users = new List<User>
            {
                BuildUser(email: "one@example.com"),
                BuildUser(email: "two@example.com")
            };
            _mockUserRepository.Setup(r => r.GetAllAsync(1, 10)).ReturnsAsync((users, 2));

            var result = await _authService.GetAllUsersAsync(1, 10);

            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(10);
            _mockUserRepository.Verify(r => r.GetAllAsync(1, 10), Times.Once);
        }

        [Test]
        public async Task GetAllUsersAsync_InvalidPage_ShouldDefaultToPage1()
        {
            _mockUserRepository.Setup(r => r.GetAllAsync(1, 10)).ReturnsAsync((new List<User>(), 0));

            var result = await _authService.GetAllUsersAsync(-5, 10);

            _mockUserRepository.Verify(r => r.GetAllAsync(1, 10), Times.Once);
        }

        [Test]
        public async Task GetAllUsersAsync_InvalidPageSize_ShouldDefaultTo10()
        {
            _mockUserRepository.Setup(r => r.GetAllAsync(1, 10)).ReturnsAsync((new List<User>(), 0));

            var result = await _authService.GetAllUsersAsync(1, -1);

            _mockUserRepository.Verify(r => r.GetAllAsync(1, 10), Times.Once);
        }

        [Test]
        public async Task GetAllUsersAsync_ExcessivePageSize_ShouldDefaultTo10()
        {
            _mockUserRepository.Setup(r => r.GetAllAsync(1, 10)).ReturnsAsync((new List<User>(), 0));

            var result = await _authService.GetAllUsersAsync(1, 500);

            _mockUserRepository.Verify(r => r.GetAllAsync(1, 10), Times.Once);
        }

        [Test]
        public async Task GetAllUsersAsync_EmptyResult_ShouldReturnEmptyPage()
        {
            _mockUserRepository.Setup(r => r.GetAllAsync(1, 10)).ReturnsAsync((new List<User>(), 0));

            var result = await _authService.GetAllUsersAsync(1, 10);

            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.TotalPages.Should().Be(0);
        }

        // ─────────────────────────────────────────────────
        // Validator Tests
        // ─────────────────────────────────────────────────

        [Test]
        public void RegisterValidator_ValidDto_ShouldPass()
        {
            var dto = BuildRegisterDto();
            var validator = new RegisterValidator();
            var result = validator.Validate(dto);
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void RegisterValidator_ShortName_ShouldFail()
        {
            var dto = BuildRegisterDto(fullName: "A");
            var validator = new RegisterValidator();
            var result = validator.Validate(dto);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "FullName");
        }

        [Test]
        public void RegisterValidator_EmptyEmail_ShouldFail()
        {
            var dto = BuildRegisterDto(email: string.Empty);
            var validator = new RegisterValidator();
            var result = validator.Validate(dto);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Email");
        }

        [Test]
        public void RegisterValidator_InvalidEmail_ShouldFail()
        {
            var dto = BuildRegisterDto(email: "notanemail");
            var validator = new RegisterValidator();
            var result = validator.Validate(dto);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void RegisterValidator_WeakPassword_ShouldFail()
        {
            var dto = BuildRegisterDto(password: "weak");
            dto.ConfirmPassword = "weak";
            var validator = new RegisterValidator();
            var result = validator.Validate(dto);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void RegisterValidator_MismatchedPasswords_ShouldFail()
        {
            var dto = BuildRegisterDto();
            dto.ConfirmPassword = "DifferentPassword@123";
            var validator = new RegisterValidator();
            var result = validator.Validate(dto);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "ConfirmPassword");
        }

        [Test]
        public void RegisterValidator_InvalidPhone_ShouldFail()
        {
            var dto = BuildRegisterDto(phone: "1234567890");
            var validator = new RegisterValidator();
            var result = validator.Validate(dto);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Phone");
        }

        [Test]
        public void LoginValidator_ValidDto_ShouldPass()
        {
            var dto = new LoginDto { Email = "test@test.com", Password = "Password@123" };
            var validator = new LoginValidator();
            var result = validator.Validate(dto);
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void LoginValidator_MissingEmail_ShouldFail()
        {
            var dto = new LoginDto { Email = string.Empty, Password = "Password@123" };
            var validator = new LoginValidator();
            var result = validator.Validate(dto);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Email");
        }

        [Test]
        public void LoginValidator_MissingPassword_ShouldFail()
        {
            var dto = new LoginDto { Email = "test@test.com", Password = string.Empty };
            var validator = new LoginValidator();
            var result = validator.Validate(dto);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Password");
        }

        // ─────────────────────────────────────────────────
        // JwtHelper Tests
        // ─────────────────────────────────────────────────

        [Test]
        public void JwtHelper_GenerateToken_ShouldReturnNonEmptyString()
        {
            User user = BuildUser();
            string token = _jwtHelper.GenerateToken(user);
            token.Should().NotBeNullOrEmpty();
            token.Split('.').Length.Should().Be(3, "JWT should have header.payload.signature format");
        }

        [Test]
        public void JwtHelper_GetExpiryTime_ShouldBeInFuture()
        {
            var expiry = _jwtHelper.GetExpiryTime();
            expiry.Should().BeAfter(DateTime.UtcNow);
            expiry.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromMinutes(1));
        }
    }
}
