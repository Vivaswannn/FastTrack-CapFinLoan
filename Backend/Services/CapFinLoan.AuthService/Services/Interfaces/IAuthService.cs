using CapFinLoan.AuthService.DTOs.Requests;
using CapFinLoan.AuthService.DTOs.Responses;
using CapFinLoan.SharedKernel.DTOs;

namespace CapFinLoan.AuthService.Services.Interfaces
{
    /// <summary>
    /// Contract for authentication business logic.
    /// Controllers depend on this interface not the concrete class.
    /// This interface enables unit testing with Moq.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Register a new applicant account.
        /// Validates email uniqueness, hashes password, saves user.
        /// </summary>
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);

        /// <summary>
        /// Authenticate user and return JWT token.
        /// If MFA is enabled, returns RequiresOtp = true.
        /// </summary>
        Task<AuthResponseDto> LoginAsync(LoginDto dto);

        /// <summary>
        /// Verify the OTP code sent to the email and return JWT token.
        /// </summary>
        Task<AuthResponseDto> VerifyOtpAsync(VerifyOtpDto dto);

        /// <summary>
        /// Get profile of currently logged in user.
        /// </summary>
        Task<UserResponseDto> GetProfileAsync(Guid userId);

        /// <summary>
        /// Get paginated list of all users. Admin only.
        /// </summary>
        Task<PagedResponseDto<UserResponseDto>> GetAllUsersAsync(
            int page, int pageSize);

        /// <summary>
        /// Activate or deactivate a user account. Admin only.
        /// </summary>
        Task<UserResponseDto> UpdateUserStatusAsync(
            Guid userId, UpdateUserStatusDto dto);
    }
}
