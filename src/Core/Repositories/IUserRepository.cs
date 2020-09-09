using Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repositories
{
    /// <summary>
    /// User repository
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>Get user by email</summary>
        Task<IUserEntity> GetUserByUserEmailAsync(string userEmail);

        /// <summary>Get user by email abd passwordHash</summary>
        Task<IUserEntity> GetUserByUserEmailAsync(string userEmail, string passwordHash);

        /// <summary>Create initial admin</summary>
        Task CreateInitialAdminAsync();

        /// <summary>Create user</summary>
        Task CreateUserAsync(IUserEntity user);

        /// <summary>Save user</summary>
        Task<bool> UpdateUserAsync(IUserEntity user);

        /// <summary>Get list of all users</summary>
        Task<List<IUserEntity>> GetUsersAsync();

        /// <summary>Get top user record</summary>
        Task<IUserEntity> GetTopUserRecordAsync();

        /// <summary>Remove user by user email</summary>
        Task<bool> RemoveUserAsync(string userEmail);
    }
}
