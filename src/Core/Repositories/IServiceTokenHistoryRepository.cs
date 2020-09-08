using Core.Entities;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IServiceTokenHistoryRepository
    {
        Task SaveTokenHistoryAsync(IServiceTokenEntity token, string userName, string userIpAddress);
    }
}
