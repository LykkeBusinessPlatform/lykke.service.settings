using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using Core.Entities;
using Core.Repositories;

namespace AzureRepositories.Token
{
    public class TokensRepository : ITokensRepository
    {
        private readonly INoSQLTableStorage<TokenEntity> _tableStorage;

        public TokensRepository(INoSQLTableStorage<TokenEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IToken> GetAsync(string tokenId)
        {
            var pk = TokenEntity.GeneratePartitionKey();
            var rk = TokenEntity.GenerateRowKey(tokenId);

            return await _tableStorage.GetDataAsync(pk, rk);
        }

        public async Task<IToken> GetTopRecordAsync()
        {
            var pk = TokenEntity.GeneratePartitionKey();
            var result = await _tableStorage.GetTopRecordAsync(pk);
            return result;
        }

        public async Task<IEnumerable<IToken>> GetAllAsync()
        {
            var pk = TokenEntity.GeneratePartitionKey();
            return await _tableStorage.GetDataAsync(pk);
        }

        public async Task RemoveTokenAsync(string tokenId)
        {
            var pk = TokenEntity.GeneratePartitionKey();
            await _tableStorage.DeleteAsync(pk, tokenId);
        }

        public async Task SaveTokenAsync(IToken token)
        {
            if (token is TokenEntity ts)
            {
                ts.PartitionKey = TokenEntity.GeneratePartitionKey();
                ts.RowKey = token.TokenId;
            }
            else
            {
                var pk = TokenEntity.GeneratePartitionKey();
                var rk = TokenEntity.GenerateRowKey(token.TokenId);
                ts = await _tableStorage.GetDataAsync(pk, rk)
                    ?? new TokenEntity { PartitionKey = pk, RowKey = rk };

                ts.TokenId = token.TokenId;
                ts.AccessList = token.AccessList;
                ts.IpList = token.IpList;
            }

            await _tableStorage.InsertOrMergeAsync(ts);
        }
    }
}
