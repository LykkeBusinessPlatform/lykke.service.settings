using System;
using System.Threading.Tasks;
using AzureRepositories.Extensions;
using AzureStorage;
using Core.Entities;
using Core.Repositories;

namespace AzureRepositories.Token
{
    public class AccountTokenHistoryRepository : IAccountTokenHistoryRepository
    {
        private readonly INoSQLTableStorage<AccountTokenHistoryEntity> _tableStorage;

        public AccountTokenHistoryRepository(INoSQLTableStorage<AccountTokenHistoryEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task SaveTokenHistoryAsync(IToken token, string userName, string userIpAddress)
        {
            var th = new AccountTokenHistoryEntity
            {
                RowKey = DateTime.UtcNow.StorageString(),
                UserName = userName,
                AccessList = token.AccessList,
                IpList = token.IpList,
                TokenId = token.RowKey,
                UserIpAddress = userIpAddress
            };
            
            await _tableStorage.InsertOrMergeAsync(th);
        }
    }
}
