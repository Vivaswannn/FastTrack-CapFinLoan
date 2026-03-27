using System.Security.Claims;
using CapFinLoan.DocumentService.DTOs.Requests;
using CapFinLoan.DocumentService.Services.Interfaces;
using CapFinLoan.SharedKernel.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapFinLoan.DocumentService.Controllers
{
    /// <summary>
    /// Applicant-facing document endpoints.
    /// Handles upload, listing, and download of KYC documents.
    /// </summary>
    [ApiController]
    [Route("api/documents")]
    [Authorize]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(
            IDocumentService documentService,
            ILogger<DocumentController> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        // ── Claims helpers ───────────────────────────────────────────────────

        private Guid GetUserIdFromClaims()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub");
            return Guid.Parse(sub!);
        }

        private string GetEmailFromClaims()
        {
            return User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue("email")
                ?? string.Empty;
        }

        private string GetRoleFromClaims()
        {
            return User.FindFirstValue(ClaimTypes.Role)
                ?? User.FindFirstValue("role")
                ?? string.Empty;
        }

        // ── Endpoints ────────────────────────────────────────────────────────

        /// <summary>
        /// Upload a KYC document for a loan application.
        /// Accepts multipart/form-data with a file and metadata fields.
        /// Replaces any previous document of the same type.
        /// </summary>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Upload(
            IFormFile file,
            [FromForm] UploadDocumentDto dto)
        {
            var userId = GetUserIdFromClaims();
            var result = await _documentService.UploadDocumentAsync(file, dto, userId);

            return StatusCode(StatusCodes.Status201Created,
                ApiResponseDto<object>.SuccessResponse(
                    result, "Document uploaded successfully."));
        }

        /// <summary>
        /// Get all active documents for a loan application.
        /// Applicants see only their own documents; Admins see all.
        /// </summary>
        [HttpGet("{appId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByApplication([FromRoute] Guid appId)
        {
            var userId = GetUserIdFromClaims();
            var role   = GetRoleFromClaims();

            var results = await _documentService.GetDocumentsByApplicationAsync(
                appId, userId, role);

            return Ok(ApiResponseDto<object>.SuccessResponse(
                results, "Documents retrieved successfully."));
        }

        /// <summary>
        /// Download a document file by its document identifier.
        /// Returns the binary file with the correct MIME type.
        /// </summary>
        [HttpGet("file/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadFile([FromRoute] Guid id)
        {
            var userId = GetUserIdFromClaims();
            var role   = GetRoleFromClaims();

            var relativePath = await _documentService.GetFilePathAsync(id, userId, role);

            // Resolve relative path against wwwroot
            var webRootPath = HttpContext.RequestServices
                .GetRequiredService<IWebHostEnvironment>().WebRootPath;

            var fullPath = Path.Combine(webRootPath, relativePath);

            if (!System.IO.File.Exists(fullPath))
                return NotFound(ApiResponseDto<object>.FailureResponse(
                    "File not found on server.", []));

            var extension   = Path.GetExtension(fullPath).ToLower();
            var contentType = extension switch
            {
                ".pdf"  => "application/pdf",
                ".jpg"  => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png"  => "image/png",
                _       => "application/octet-stream"
            };

            return PhysicalFile(fullPath, contentType);
        }
    }
}
