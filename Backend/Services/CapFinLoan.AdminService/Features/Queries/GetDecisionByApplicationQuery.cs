using CapFinLoan.AdminService.DTOs.Responses;
using CapFinLoan.AdminService.Services.Interfaces;
using MediatR;

namespace CapFinLoan.AdminService.Features.Queries
{
    /// <summary>Returns the decision for a specific loan application.</summary>
    public record GetDecisionByApplicationQuery(Guid ApplicationId)
        : IRequest<DecisionResponseDto?>;

    /// <summary>Handles <see cref="GetDecisionByApplicationQuery"/>.</summary>
    public class GetDecisionByApplicationQueryHandler
        : IRequestHandler<GetDecisionByApplicationQuery, DecisionResponseDto?>
    {
        private readonly IAdminService _adminService;

        public GetDecisionByApplicationQueryHandler(IAdminService adminService)
            => _adminService = adminService;

        /// <inheritdoc/>
        public Task<DecisionResponseDto?> Handle(
            GetDecisionByApplicationQuery request,
            CancellationToken cancellationToken)
            => _adminService.GetDecisionByApplicationAsync(request.ApplicationId);
    }
}
