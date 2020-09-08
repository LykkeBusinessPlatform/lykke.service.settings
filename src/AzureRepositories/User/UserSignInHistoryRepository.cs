using System;
using System.Threading.Tasks;
using AzureStorage;
using Core.Entities;
using Core.Repositories;

namespace AzureRepositories.User
{
    public class UserSignInHistoryRepository : IUserSignInHistoryRepository
    {

        private readonly INoSQLTableStorage<UserSignInHistoryEntity> _tableStorage;

        public UserSignInHistoryRepository(INoSQLTableStorage<UserSignInHistoryEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task SaveUserLoginHistoryAsync(IUserEntity user, string userIpAddress)
        {
            var uh = new UserSignInHistoryEntity
            {
                PartitionKey = UserSignInHistoryEntity.GeneratePartitionKey(),

                UserEmail = user.RowKey,
                SignInDate = DateTime.UtcNow,
                IpAddress = userIpAddress

            };

            uh.RowKey = uh.GetRawKey();

            await _tableStorage.InsertOrMergeAsync(uh);
        }
    }
}
