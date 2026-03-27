using CapFinLoan.AdminService.DTOs.Responses;
using CapFinLoan.AdminService.Services.Interfaces;
using MediatR;

namespace CapFinLoan.AdminService.Features.Queries
{
    /// <summary>Returns aggregated KPI statistics for the admin dashboard.</summary>
    public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;

    /// <summary>Handles <see cref="GetDashboardStatsQuery"/>.</summary>
    public class GetDashboardStatsQueryHandler
        : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
    {
        private readonly IAdminService _adminService;

        public GetDashboardStatsQueryHandler(IAdminService adminService)
            => _adminService = adminService;

        /// <inheritdoc/>
        public Task<DashboardStatsDto> Handle(
            GetDashboardStatsQuery request,
            CancellationToken cancellationToken)
            => _adminService.GetDashboardStatsAsync();
    }
}
