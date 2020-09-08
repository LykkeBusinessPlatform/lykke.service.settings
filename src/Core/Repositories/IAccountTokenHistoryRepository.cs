using Core.Entities;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IAccountTokenHistoryRepository
    {
        Task SaveTokenHistoryAsync(IToken token, string userName, string userIpAddress);
    }
}
