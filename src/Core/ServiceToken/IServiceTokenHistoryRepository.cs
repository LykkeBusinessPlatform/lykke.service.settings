using System.Threading.Tasks;

namespace Core.ServiceToken
{
    public interface IServiceTokenHistoryRepository
    {
        Task SaveTokenHistoryAsync(IServiceTokenEntity token, string userName, string userIpAddress);
    }
}
