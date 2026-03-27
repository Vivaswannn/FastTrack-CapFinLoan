using CapFinLoan.AdminService.Services.Interfaces;
using MediatR;

namespace CapFinLoan.AdminService.Features.Commands
{
    /// <summary>
    /// Command to export all decisions (or a date-filtered subset) as a CSV byte array.
    /// Treated as a command because it creates a Report record as a side-effect.
    /// </summary>
    public record ExportDecisionsCsvCommand(
        DateTime? StartDate,
        DateTime? EndDate) : IRequest<byte[]>;

    /// <summary>Handles <see cref="ExportDecisionsCsvCommand"/>.</summary>
    public class ExportDecisionsCsvCommandHandler
        : IRequestHandler<ExportDecisionsCsvCommand, byte[]>
    {
        private readonly IAdminService _adminService;

        public ExportDecisionsCsvCommandHandler(IAdminService adminService)
            => _adminService = adminService;

        /// <inheritdoc/>
        public Task<byte[]> Handle(
            ExportDecisionsCsvCommand request,
            CancellationToken cancellationToken)
            => _adminService.ExportDecisionsToCsvAsync(
                request.StartDate,
                request.EndDate);
    }
}
