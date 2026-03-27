using CapFinLoan.PaymentService.Services.Interfaces;
using CapFinLoan.SharedKernel.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapFinLoan.PaymentService.Controllers
{
    /// <summary>
    /// REST endpoints to query payment history.
    /// Write operations are event-driven (Saga pattern) and not exposed via HTTP.
    /// </summary>
    [ApiController]
    [Route("api/payments")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IPaymentService paymentService,
            ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _logger         = logger;
        }

        /// <summary>
        /// Returns all payment records for a given loan application.
        /// </summary>
        [HttpGet("application/{applicationId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByApplication(
            [FromRoute] Guid applicationId)
        {
            _logger.LogInformation(
                "GetByApplication called for ApplicationId={ApplicationId}",
                applicationId);

            var payments = await _paymentService
                .GetPaymentsByApplicationAsync(applicationId);

            return Ok(ApiResponseDto<object>.SuccessResponse(
                payments, "Payments retrieved successfully."));
        }

        /// <summary>
        /// Returns a single payment record by its ID.
        /// </summary>
        [HttpGet("{paymentId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(
            [FromRoute] Guid paymentId)
        {
            var payment = await _paymentService.GetPaymentByIdAsync(paymentId);

            if (payment is null)
                return NotFound(ApiResponseDto<object>.FailureResponse(
                    "Payment not found.", new List<string>()));

            return Ok(ApiResponseDto<object>.SuccessResponse(
                payment, "Payment retrieved successfully."));
        }
    }
}
