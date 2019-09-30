using System.Threading.Tasks;

namespace Core.User
{
    public interface IUserSignInHistoryRepository
    {
        Task SaveUserLoginHistoryAsync(IUserEntity user, string userIpAddress);
    }
}
