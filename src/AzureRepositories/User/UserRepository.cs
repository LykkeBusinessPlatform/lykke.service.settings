using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.Entities;
using Core.Repositories;

namespace AzureRepositories.User
{
    public class UserRepository : IUserRepository
    {
        private readonly INoSQLTableStorage<UserEntity> _tableStorage;

        public UserRepository(INoSQLTableStorage<UserEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IUserEntity> GetUserByUserEmailAsync(string userEmail)
        {
            var pk = UserEntity.GeneratePartitionKey();
            return await _tableStorage.GetDataAsync(pk, UserEntity.GenerateRowKey(userEmail));
        }

        public async Task<IUserEntity> GetUserByUserEmailAsync(string userEmail, string passwordHash)
        {
            var pk = UserEntity.GeneratePartitionKey();
            var result = await _tableStorage.GetDataAsync(pk, UserEntity.GenerateRowKey(userEmail));
            if (result == null)
                return null;

            return result.PasswordHash.Equals(passwordHash) ? result : null;
        }

        public async Task CreateInitialAdminAsync(string defaultUserEmail, string defaultUserPasswordHash)
        {
            var usr = new UserEntity
            {
                PartitionKey = UserEntity.GeneratePartitionKey(),
                RowKey = defaultUserEmail,
                Email = defaultUserEmail,
                PasswordHash = defaultUserPasswordHash,
                FirstName = "Admin",
                LastName = "Initial",
                Active = true,
                Admin = true
            };
            await _tableStorage.InsertOrMergeAsync(usr);
        }

        public Task CreateUserAsync(IUserEntity user)
        {
            var email = UserEntity.GenerateRowKey(user.Email);
            var usr = new UserEntity
            {
                PartitionKey = UserEntity.GeneratePartitionKey(),
                RowKey = email,
                Email = email,
                PasswordHash = user.PasswordHash,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Active = user.Active,
                Admin = user.Admin,
                Roles = user.Roles,
            };

            return _tableStorage.InsertOrMergeAsync(usr);
        }

        public async Task<bool> UpdateUserAsync(IUserEntity user)
        {
            try
            {
                var te = (UserEntity)user;
                if (te.PartitionKey == null)
                    te.PartitionKey = UserEntity.GeneratePartitionKey();
                te.RowKey = UserEntity.GenerateRowKey(te.Email);

                await _tableStorage.InsertOrMergeAsync(te);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public async Task<List<IUserEntity>> GetUsersAsync()
        {
            var pk = UserEntity.GeneratePartitionKey();
            var users = await _tableStorage.GetDataAsync(pk);
            return users.Cast<IUserEntity>().ToList();
        }

        public async Task<IUserEntity> GetTopUserRecordAsync()
        {
            var pk = UserEntity.GeneratePartitionKey();
            var result = await _tableStorage.GetTopRecordAsync(pk);
            return result;
        }

        public async Task<bool> RemoveUserAsync(string userEmail)
        {
            try
            {
                await _tableStorage.DeleteAsync(UserEntity.GeneratePartitionKey(), UserEntity.GenerateRowKey(userEmail));
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
