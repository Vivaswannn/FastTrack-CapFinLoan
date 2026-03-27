using System.Collections.Generic;
using CapFinLoan.SharedKernel.DTOs;

namespace CapFinLoan.SharedKernel.Helpers
{
    /// <summary>
    /// Helper class for creating paginated responses.
    /// Use this in all repository methods that return lists.
    /// </summary>
    public static class PaginationHelper
    {
        /// <summary>
        /// Creates a paged response from a list of items
        /// </summary>
        /// <param name="items">Items for current page only</param>
        /// <param name="totalCount">Total records in database</param>
        /// <param name="page">Current page number</param>
        /// <param name="pageSize">Items per page</param>
        public static PagedResponseDto<T> CreatePagedResponse<T>(
            List<T> items,
            int totalCount,
            int page,
            int pageSize)
        {
            return new PagedResponseDto<T>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
