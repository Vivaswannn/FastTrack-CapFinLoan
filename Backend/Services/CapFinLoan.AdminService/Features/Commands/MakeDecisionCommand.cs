using CapFinLoan.AdminService.DTOs.Requests;
using CapFinLoan.AdminService.DTOs.Responses;
using CapFinLoan.AdminService.Services.Interfaces;
using MediatR;

namespace CapFinLoan.AdminService.Features.Commands
{
    /// <summary>
    /// Command to approve or reject a loan application.
    /// Encapsulates all inputs needed by the AdminService decision flow.
    /// </summary>
    public record MakeDecisionCommand(
        Guid ApplicationId,
        MakeDecisionDto Dto,
        Guid AdminId,
        string AdminEmail,
        string AdminToken) : IRequest<DecisionResponseDto>;

    /// <summary>Handles <see cref="MakeDecisionCommand"/>.</summary>
    public class MakeDecisionCommandHandler
        : IRequestHandler<MakeDecisionCommand, DecisionResponseDto>
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<MakeDecisionCommandHandler> _logger;

        public MakeDecisionCommandHandler(
            IAdminService adminService,
            ILogger<MakeDecisionCommandHandler> logger)
        {
            _adminService = adminService;
            _logger       = logger;
        }

        /// <inheritdoc/>
        public async Task<DecisionResponseDto> Handle(
            MakeDecisionCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "MakeDecisionCommand received for ApplicationId={ApplicationId} DecisionType={Type}",
                request.ApplicationId, request.Dto.DecisionType);

            return await _adminService.MakeDecisionAsync(
                request.ApplicationId,
                request.Dto,
                request.AdminId,
                request.AdminEmail,
                request.AdminToken);
        }
    }
}
