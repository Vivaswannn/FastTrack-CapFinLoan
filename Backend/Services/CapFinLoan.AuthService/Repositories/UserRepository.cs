using CapFinLoan.AuthService.Data;
using CapFinLoan.AuthService.Models;
using CapFinLoan.AuthService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CapFinLoan.AuthService.Repositories
{
    /// <summary>
    /// Handles all database operations for Users table.
    /// This is the ONLY class that directly uses AuthDbContext.
    /// No business logic here — only data access.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly AuthDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(
            AuthDbContext context,
            ILogger<UserRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<User?> GetByIdAsync(Guid userId)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        /// <inheritdoc/>
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.Email.ToLower() == email.ToLower());
        }

        /// <inheritdoc/>
        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }

        /// <inheritdoc/>
        public async Task<User> CreateAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation(
                "New user created: {Email} with role {Role}",
                user.Email, user.Role);
            return user;
        }

        /// <inheritdoc/>
        public async Task<User> UpdateAsync(User user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation(
                "User updated: {Email}", user.Email);
            return user;
        }

        /// <inheritdoc/>
        public async Task<(List<User> Users, int TotalCount)> GetAllAsync(
            int page, int pageSize)
        {
            var query = _context.Users
                .AsNoTracking()
                .OrderByDescending(u => u.CreatedAt);

            var totalCount = await query.CountAsync();

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users, totalCount);
        }
    }
}
