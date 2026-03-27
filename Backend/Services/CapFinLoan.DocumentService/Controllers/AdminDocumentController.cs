using System.Security.Claims;
using CapFinLoan.DocumentService.DTOs.Requests;
using CapFinLoan.DocumentService.Services.Interfaces;
using CapFinLoan.SharedKernel.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapFinLoan.DocumentService.Controllers
{
    /// <summary>
    /// Admin-only document endpoints.
    /// Handles KYC verification and audit views.
    /// </summary>
    [ApiController]
    [Route("api/admin/documents")]
    [Authorize(Roles = "Admin")]
    public class AdminDocumentController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<AdminDocumentController> _logger;

        public AdminDocumentController(
            IDocumentService documentService,
            ILogger<AdminDocumentController> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        private string GetEmailFromClaims()
        {
            return User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue("email")
                ?? string.Empty;
        }

        /// <summary>
        /// Verify or reject a KYC document.
        /// Rejection requires a reason in VerificationRemarks.
        /// </summary>
        [HttpPut("{id:guid}/verify")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> VerifyDocument(
            [FromRoute] Guid id,
            [FromBody] VerifyDocumentDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponseDto<object>.FailureResponse(
                    "Validation failed.", errors));
            }

            var adminEmail = GetEmailFromClaims();
            var result = await _documentService.VerifyDocumentAsync(id, dto, adminEmail);

            return Ok(ApiResponseDto<object>.SuccessResponse(
                result, "Document verification updated successfully."));
        }

        /// <summary>
        /// Get all documents (including replaced ones) for a loan application.
        /// Used for admin audit and review.
        /// </summary>
        [HttpGet("{appId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllDocuments([FromRoute] Guid appId)
        {
            var results = await _documentService.GetAllDocumentsByApplicationAsync(appId);

            return Ok(ApiResponseDto<object>.SuccessResponse(
                results, "Documents retrieved successfully."));
        }
    }
}
