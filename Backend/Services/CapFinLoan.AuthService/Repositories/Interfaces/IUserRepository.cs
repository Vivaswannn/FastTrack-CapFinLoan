using CapFinLoan.AuthService.Models;

namespace CapFinLoan.AuthService.Repositories.Interfaces
{
    /// <summary>
    /// Contract for user data access operations.
    /// All database operations for Users table go through here.
    /// Services depend on this interface not the concrete class.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>Get user by unique ID</summary>
        Task<User?> GetByIdAsync(Guid userId);

        /// <summary>Get user by email address</summary>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>Check if email already exists in database</summary>
        Task<bool> EmailExistsAsync(string email);

        /// <summary>Save new user to database</summary>
        Task<User> CreateAsync(User user);

        /// <summary>Update existing user in database</summary>
        Task<User> UpdateAsync(User user);

        /// <summary>
        /// Get paginated list of all users.
        /// Used by admin user management page.
        /// </summary>
        Task<(List<User> Users, int TotalCount)> GetAllAsync(
            int page, int pageSize);
    }
}
