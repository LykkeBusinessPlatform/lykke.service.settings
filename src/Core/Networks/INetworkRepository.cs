using System.Threading.Tasks;

namespace Core.Networks
{
    public interface INetworkRepository
    {
        Task<Network[]> GetAllAsync();
        Task<Network> GetByIpAsync(string ip);
        Task<bool> NetworkExistsAsync(string id);
        Task AddAsync(INetwork network);
        Task UpdateAsync(INetwork network);
        Task DeleteAsync(string id);
    }
}
