using CapFinLoan.AdminService.DTOs.Responses;
using CapFinLoan.AdminService.Services.Interfaces;
using MediatR;

namespace CapFinLoan.AdminService.Features.Queries
{
    /// <summary>Returns monthly decision counts for trend charts.</summary>
    public record GetMonthlyTrendQuery(int Months) : IRequest<List<MonthlyTrendDto>>;

    /// <summary>Handles <see cref="GetMonthlyTrendQuery"/>.</summary>
    public class GetMonthlyTrendQueryHandler
        : IRequestHandler<GetMonthlyTrendQuery, List<MonthlyTrendDto>>
    {
        private readonly IAdminService _adminService;

        public GetMonthlyTrendQueryHandler(IAdminService adminService)
            => _adminService = adminService;

        /// <inheritdoc/>
        public Task<List<MonthlyTrendDto>> Handle(
            GetMonthlyTrendQuery request,
            CancellationToken cancellationToken)
            => _adminService.GetMonthlyTrendAsync(request.Months);
    }
}
