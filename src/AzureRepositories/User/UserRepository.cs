using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.User;

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
            {
                return null;
            }
            return result.PasswordHash.Equals(passwordHash) ? result : null;
        }

        public async Task<bool> SaveUserAsync(IUserEntity user)
        {
            try
            {
                var te = (UserEntity)user;
                te.RowKey = UserEntity.GenerateRowKey(te.RowKey);
                if (te.PartitionKey == null)
                {
                    te.PartitionKey = UserEntity.GeneratePartitionKey();
                }
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
