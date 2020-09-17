using System;
using System.Threading.Tasks;
using AzureStorage;
using Core.Entities;
using Core.Repositories;

namespace AzureRepositories.Lock
{
    public class LockRepository : ILockRepository
    {
        private readonly INoSQLTableStorage<LockEntity> _tableStorage;
        private const string JsonLockKey = "jsonLock";

        public LockRepository(INoSQLTableStorage<LockEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<ILockEntity> GetJsonPageLockAsync()
        {
            var pk = LockEntity.GeneratePartitionKey();
            return await _tableStorage.GetDataAsync(pk, JsonLockKey);
        }

        public async Task SetJsonPageLockAsync(string userEmail, string userName, string ipAddress)
        {
            await _tableStorage.InsertOrMergeAsync(new LockEntity
            {
                PartitionKey = LockEntity.GeneratePartitionKey(),
                RowKey = JsonLockKey,
                UserEmail = userEmail,
                DateTime = DateTime.UtcNow,
                UserName = userName,
                IpAddress = ipAddress,
            });
        }

        public async Task ResetJsonPageLockAsync()
        {
            await _tableStorage.InsertOrReplaceAsync(new LockEntity
            {
                PartitionKey = LockEntity.GeneratePartitionKey(),
                RowKey = JsonLockKey,
                DateTime = new DateTime(1701, 1, 1), //Storage Azure can't store less
            });
        }
    }
}
