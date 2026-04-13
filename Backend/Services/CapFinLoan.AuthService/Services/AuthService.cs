using CapFinLoan.AuthService.DTOs.Requests;
using CapFinLoan.AuthService.DTOs.Responses;
using CapFinLoan.AuthService.Helpers;
using CapFinLoan.AuthService.Models;
using CapFinLoan.AuthService.Repositories.Interfaces;
using CapFinLoan.AuthService.Services.Interfaces;
using CapFinLoan.SharedKernel.DTOs;
using CapFinLoan.SharedKernel.Helpers;
using CapFinLoan.AuthService.Messaging;

namespace CapFinLoan.AuthService.Services
{
    /// <summary>
    /// Handles all authentication business logic.
    /// Depends on IUserRepository for data access.
    /// Never uses DbContext directly.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtHelper _jwtHelper;
        private readonly IMessagePublisher _messagePublisher;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            JwtHelper jwtHelper,
            IMessagePublisher messagePublisher,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _jwtHelper = jwtHelper;
            _messagePublisher = messagePublisher;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            _logger.LogInformation(
                "Registration attempt for email: {Email}", dto.Email);

            // Check if email already exists
            bool emailExists = await _userRepository
                .EmailExistsAsync(dto.Email);

            if (emailExists)
            {
                _logger.LogWarning(
                    "Registration failed — duplicate email: {Email}",
                    dto.Email);
                throw new ArgumentException(
                    "An account with this email address already exists.");
            }

            // Create new user with hashed password
            User user = new User
            {
                FullName = dto.FullName.Trim(),
                Email = dto.Email.ToLower().Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Phone = dto.Phone.Trim(),
                Role = "Applicant",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            User createdUser = await _userRepository.CreateAsync(user);

            _logger.LogInformation(
                "New applicant registered successfully: {Email}",
                createdUser.Email);

            // Generate JWT token so user is immediately logged in
            string token = _jwtHelper.GenerateToken(createdUser);

            return new AuthResponseDto
            {
                Token = token,
                ExpiresAt = _jwtHelper.GetExpiryTime(),
                UserId = createdUser.UserId,
                FullName = createdUser.FullName,
                Email = createdUser.Email,
                Role = createdUser.Role
            };
        }

        /// <inheritdoc/>
        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            _logger.LogInformation(
                "Login attempt for email: {Email}", dto.Email);

            // Find user by email
            User? user = await _userRepository
                .GetByEmailAsync(dto.Email);

            if (user == null)
            {
                _logger.LogWarning(
                    "Login failed — user not found: {Email}", dto.Email);
                // Generic message — do not reveal whether email exists
                throw new UnauthorizedAccessException(
                    "Invalid credentials.");
            }

            // Check if account is active
            if (!user.IsActive)
            {
                _logger.LogWarning(
                    "Login failed — account inactive: {Email}", dto.Email);
                throw new UnauthorizedAccessException(
                    "Your account has been deactivated. " +
                    "Please contact support.");
            }

            // Verify password against BCrypt hash
            bool passwordValid = BCrypt.Net.BCrypt
                .Verify(dto.Password, user.PasswordHash);

            if (!passwordValid)
            {
                _logger.LogWarning(
                    "Login failed — wrong password: {Email}", dto.Email);
                // Generic message — do not reveal which field was wrong
                throw new UnauthorizedAccessException(
                    "Invalid credentials.");
            }

            _logger.LogInformation(
                "User authenticated successfully: {Email} Role: {Role}",
                user.Email, user.Role);

            // Generate JWT and return immediately
            string token = _jwtHelper.GenerateToken(user);

            return new AuthResponseDto
            {
                Token = token,
                ExpiresAt = _jwtHelper.GetExpiryTime(),
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                RequiresOtp = false
            };
        }

        private string DefaultOtpFallbackForDev(string otp, string email)
        {
            _logger.LogInformation($"[DEV ONLY] OTP for {email} is {otp}");
            return otp;
        }

        /// <inheritdoc/>
        public async Task<AuthResponseDto> VerifyOtpAsync(VerifyOtpDto dto)
        {
            _logger.LogInformation("OTP verify attempt for email: {Email}", dto.Email);

            User? user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null || !user.IsActive)
                 throw new UnauthorizedAccessException("Invalid request.");

            if (user.OtpCode != dto.OtpCode)
            {
                throw new UnauthorizedAccessException("Invalid OTP code.");
            }

            if (user.OtpExpiry < DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("OTP has expired. Please login again.");
            }

            // Clear OTP
            user.OtpCode = null;
            user.OtpExpiry = null;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User OTP verified successfully: {Email} Role: {Role}", user.Email, user.Role);

            string token = _jwtHelper.GenerateToken(user);
            return new AuthResponseDto
            {
                Token = token,
                ExpiresAt = _jwtHelper.GetExpiryTime(),
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                RequiresOtp = false
            };
        }

        /// <inheritdoc/>
        public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            _logger.LogInformation("Forgot password request for: {Email}", dto.Email);

            User? user = await _userRepository.GetByEmailAsync(dto.Email);

            // Always return success — do not reveal whether email exists
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Forgot password — account not found or inactive: {Email}", dto.Email);
                return;
            }

            string otp = new Random().Next(100000, 999999).ToString();
            user.OtpCode = otp;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("[DEV ONLY] Password reset OTP for {Email} is {Otp}", user.Email, otp);

            await _messagePublisher.PublishOtpRequestedAsync(new SharedKernel.Events.OtpRequestedEvent
            {
                UserId = user.UserId,
                Email = user.Email,
                FullName = user.FullName,
                OtpCode = otp,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <inheritdoc/>
        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            _logger.LogInformation("Password reset attempt for: {Email}", dto.Email);

            User? user = await _userRepository.GetByEmailAsync(dto.Email);

            if (user == null || !user.IsActive)
                throw new UnauthorizedAccessException("Invalid request.");

            if (user.OtpCode != dto.OtpCode)
                throw new UnauthorizedAccessException("Invalid OTP code.");

            if (user.OtpExpiry < DateTime.UtcNow)
                throw new UnauthorizedAccessException("OTP has expired. Please request a new one.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.OtpCode = null;
            user.OtpExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Password reset successfully for: {Email}", user.Email);
        }

        /// <inheritdoc/>
        public async Task<UserResponseDto> GetProfileAsync(Guid userId)
        {
            User? user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                throw new KeyNotFoundException(
                    $"User with ID {userId} not found.");
            }

            return MapToUserResponseDto(user);
        }

        /// <inheritdoc/>
        public async Task<PagedResponseDto<UserResponseDto>> GetAllUsersAsync(
            int page, int pageSize)
        {
            // Validate pagination params
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            (List<User> users, int totalCount) = await _userRepository
                .GetAllAsync(page, pageSize);

            List<UserResponseDto> userDtos = users
                .Select(u => MapToUserResponseDto(u))
                .ToList();

            return PaginationHelper.CreatePagedResponse(
                userDtos, totalCount, page, pageSize);
        }

        /// <inheritdoc/>
        public async Task<UserResponseDto> UpdateUserStatusAsync(
            Guid userId, UpdateUserStatusDto dto)
        {
            User? user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                throw new KeyNotFoundException(
                    $"User with ID {userId} not found.");
            }

            // Prevent deactivating the main admin account
            if (user.Email == "admin@capfinloan.com" && !dto.IsActive)
            {
                throw new InvalidOperationException(
                    "Cannot deactivate the system admin account.");
            }

            user.IsActive = dto.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            User updatedUser = await _userRepository.UpdateAsync(user);

            _logger.LogInformation(
                "User {Email} status changed to {Status} by admin",
                user.Email, dto.IsActive ? "Active" : "Inactive");

            return MapToUserResponseDto(updatedUser);
        }

        /// <summary>
        /// Maps a User model to UserResponseDto.
        /// Private helper — used internally only.
        /// </summary>
        private static UserResponseDto MapToUserResponseDto(User user)
        {
            return new UserResponseDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }
    }
}
