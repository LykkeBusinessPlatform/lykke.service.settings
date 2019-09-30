using System.Threading.Tasks;

namespace Core.Token
{
    public interface IAccountTokenHistoryRepository
    {
        Task SaveTokenHistoryAsync(IToken token, string userName, string userIpAddress);
    }
}
