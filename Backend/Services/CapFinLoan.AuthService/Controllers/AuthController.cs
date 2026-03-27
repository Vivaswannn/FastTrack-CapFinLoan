using System.Security.Claims;
using CapFinLoan.AuthService.DTOs.Requests;
using CapFinLoan.AuthService.Services.Interfaces;
using CapFinLoan.SharedKernel.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapFinLoan.AuthService.Controllers
{
    /// <summary>
    /// Handles authentication and user management endpoints.
    /// Public endpoints: Register, Login
    /// Protected endpoints: Profile, Users (Admin only)
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Register a new applicant account.
        /// Public endpoint — no authentication required.
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register(
            [FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                List<string> errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(
                    ApiResponseDto<object>.FailureResponse(
                        "Validation failed.", errors));
            }

            var result = await _authService.RegisterAsync(dto);

            return StatusCode(201,
                ApiResponseDto<object>.SuccessResponse(
                    result,
                    "Account created successfully. Welcome to CapFinLoan!"));
        }

        /// <summary>
        /// Login with email and password. Returns JWT token.
        /// Public endpoint — no authentication required.
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(
            [FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                List<string> errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(
                    ApiResponseDto<object>.FailureResponse(
                        "Validation failed.", errors));
            }

            var result = await _authService.LoginAsync(dto);

            if (result.RequiresOtp)
            {
                 return Ok(
                     ApiResponseDto<object>.SuccessResponse(
                         result,
                         $"An OTP has been sent to your email. Please verify to continue."));
            }

            return Ok(
                ApiResponseDto<object>.SuccessResponse(
                    result,
                    $"Welcome back, {result.FullName}!"));
        }

        /// <summary>
        /// Verify OTP for MFA. Returns JWT token on success.
        /// Public endpoint — no authentication required.
        /// </summary>
        [HttpPost("verify-otp")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> VerifyOtp(
            [FromBody] VerifyOtpDto dto)
        {
            if (!ModelState.IsValid)
            {
                List<string> errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(
                    ApiResponseDto<object>.FailureResponse(
                        "Validation failed.", errors));
            }

            var result = await _authService.VerifyOtpAsync(dto);

            return Ok(
                ApiResponseDto<object>.SuccessResponse(
                    result,
                    $"Welcome back, {result.FullName}!"));
        }

        /// <summary>
        /// Get profile of currently authenticated user.
        /// Requires valid JWT token.
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetProfile()
        {
            // Get UserId from JWT claims
            Claim? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("sub");

            if (userIdClaim == null || !Guid.TryParse(
                userIdClaim.Value, out Guid userId))
            {
                return Unauthorized(
                    ApiResponseDto<object>.FailureResponse(
                        "Invalid token claims."));
            }

            var result = await _authService.GetProfileAsync(userId);

            return Ok(
                ApiResponseDto<object>.SuccessResponse(
                    result, "Profile retrieved successfully."));
        }

        /// <summary>
        /// Get paginated list of all users.
        /// Admin only endpoint.
        /// </summary>
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _authService
                .GetAllUsersAsync(page, pageSize);

            return Ok(
                ApiResponseDto<object>.SuccessResponse(
                    result, "Users retrieved successfully."));
        }

        /// <summary>
        /// Activate or deactivate a user account.
        /// Admin only endpoint.
        /// </summary>
        [HttpPut("users/{userId}/status")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateUserStatus(
            Guid userId,
            [FromBody] UpdateUserStatusDto dto)
        {
            var result = await _authService
                .UpdateUserStatusAsync(userId, dto);

            return Ok(
                ApiResponseDto<object>.SuccessResponse(
                    result,
                    $"User account {(dto.IsActive ? "activated" : "deactivated")} successfully."));
        }
    }
}
