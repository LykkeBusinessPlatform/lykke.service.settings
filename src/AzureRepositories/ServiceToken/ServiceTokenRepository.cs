using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.Entities;
using Core.Repositories;

namespace AzureRepositories.ServiceToken
{
    public class ServiceTokenRepository : IServiceTokenRepository
    {
        private readonly INoSQLTableStorage<ServiceTokenEntity> _tableStorage;

        public ServiceTokenRepository(INoSQLTableStorage<ServiceTokenEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<List<IServiceTokenEntity>> GetAllAsync()
        {
            var pk = ServiceTokenEntity.GeneratePartitionKey();
            var tokens = await _tableStorage.GetDataAsync(pk);
            return tokens.Cast<IServiceTokenEntity>().ToList();
        }

        public async Task<IServiceTokenEntity> GetTopRecordAsync()
        {
            var pk = ServiceTokenEntity.GeneratePartitionKey();
            var result = await _tableStorage.GetTopRecordAsync(pk);
            return result;
        }

        public async Task<IServiceTokenEntity> GetAsync(string tokenKey)
        {
            var pk = ServiceTokenEntity.GeneratePartitionKey();
            return await _tableStorage.GetDataAsync(pk, tokenKey);
        }

        public async Task<bool> SaveOrUpdateAsync(IServiceTokenEntity token)
        {
            try
            {
                var pk = ServiceTokenEntity.GeneratePartitionKey();
                var sToken = await _tableStorage.GetDataAsync(pk, token.Token);
                if (sToken == null)
                {
                    sToken = new ServiceTokenEntity
                    {
                        PartitionKey = ServiceTokenEntity.GeneratePartitionKey(),
                        RowKey = token.Token,
                        Token = token.Token,
                    };
                }

                sToken.SecurityKeyOne = token.SecurityKeyOne;
                sToken.SecurityKeyTwo = token.SecurityKeyTwo;
                await _tableStorage.InsertOrMergeAsync(sToken);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public async Task<bool> RemoveAsync(string tokenId)

        {
            try
            {
                var pk = ServiceTokenEntity.GeneratePartitionKey();
                await _tableStorage.DeleteAsync(pk, tokenId);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
