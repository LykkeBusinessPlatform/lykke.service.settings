using Core.Entities;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IUserSignInHistoryRepository
    {
        Task SaveUserLoginHistoryAsync(IUserEntity user, string userIpAddress);
    }
}
