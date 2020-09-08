using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.ServiceToken;

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

        public async Task<bool> SaveAsync(IServiceTokenEntity token)
        {
            try
            {
                var pk = ServiceTokenEntity.GeneratePartitionKey();
                var sToken = await _tableStorage.GetDataAsync(pk, token.RowKey);
                var sNewToken = (ServiceTokenEntity)token;

                if (sToken == null)
                {
                    sToken = new ServiceTokenEntity
                    {
                        PartitionKey = ServiceTokenEntity.GeneratePartitionKey(),
                        RowKey = token.RowKey,
                        ETag = token.ETag
                    };
                }

                sToken.SecurityKeyOne = sNewToken.SecurityKeyOne;
                sToken.SecurityKeyTwo = sNewToken.SecurityKeyTwo;
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
