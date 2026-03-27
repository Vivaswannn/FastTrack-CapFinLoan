namespace CapFinLoan.SharedKernel.DTOs;

/// <summary>
/// Standard API response wrapper used by every endpoint across all services.
/// </summary>
/// <typeparam name="T">The type of the response data payload.</typeparam>
public class ApiResponseDto<T>
{
    /// <summary>Gets or sets whether the request succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the human-readable message describing the result.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the response data payload.</summary>
    public T? Data { get; set; }

    /// <summary>Gets or sets the list of validation or error messages.</summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// Creates a successful response with data and message.
    /// </summary>
    /// <param name="data">The payload to return.</param>
    /// <param name="message">The success message.</param>
    /// <returns>A populated <see cref="ApiResponseDto{T}"/> with Success = true.</returns>
    public static ApiResponseDto<T> SuccessResponse(T data, string message = "Request completed successfully.")
    {
        return new ApiResponseDto<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Errors = []
        };
    }

    /// <summary>
    /// Creates a failure response with a message and optional error list.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <param name="errors">Optional list of detailed error strings.</param>
    /// <returns>A populated <see cref="ApiResponseDto{T}"/> with Success = false.</returns>
    public static ApiResponseDto<T> FailureResponse(string message, List<string>? errors = null)
    {
        return new ApiResponseDto<T>
        {
            Success = false,
            Message = message,
            Data = default,
            Errors = errors ?? []
        };
    }
}
