using Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IServiceTokenRepository
    {

        Task<List<IServiceTokenEntity>> GetAllAsync();
        Task<IServiceTokenEntity> GetTopRecordAsync();
        Task<IServiceTokenEntity> GetAsync(string tokenKey);
        Task<bool> SaveOrUpdateAsync(IServiceTokenEntity token);
        Task<bool> RemoveAsync(string tokenId);
    }
}
