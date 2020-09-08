using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.ServiceToken
{
    public interface IServiceTokenRepository
    {

        Task<List<IServiceTokenEntity>> GetAllAsync();
        Task<IServiceTokenEntity> GetTopRecordAsync();
        Task<IServiceTokenEntity> GetAsync(string tokenKey);
        Task<bool> SaveAsync(IServiceTokenEntity token);
        Task<bool> RemoveAsync(string tokenId);
    }
}
