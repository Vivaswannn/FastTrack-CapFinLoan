using CapFinLoan.AdminService.Services.Interfaces;
using MediatR;

namespace CapFinLoan.AdminService.Features.Queries
{
    /// <summary>Retrieves the paginated application queue from ApplicationService.</summary>
    public record GetApplicationQueueQuery(
        int Page,
        int PageSize,
        string? StatusFilter,
        string AdminToken) : IRequest<string>;

    /// <summary>Handles <see cref="GetApplicationQueueQuery"/>.</summary>
    public class GetApplicationQueueQueryHandler
        : IRequestHandler<GetApplicationQueueQuery, string>
    {
        private readonly IAdminService _adminService;

        public GetApplicationQueueQueryHandler(IAdminService adminService)
            => _adminService = adminService;

        /// <inheritdoc/>
        public Task<string> Handle(
            GetApplicationQueueQuery request,
            CancellationToken cancellationToken)
            => _adminService.GetApplicationQueueAsync(
                request.Page,
                request.PageSize,
                request.StatusFilter,
                request.AdminToken);
    }
}
