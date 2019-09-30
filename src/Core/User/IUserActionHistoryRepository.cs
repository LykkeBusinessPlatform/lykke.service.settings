using System.Threading.Tasks;

namespace Core.User
{
    public interface IUserActionHistoryRepository
    {
        Task SaveUserActionHistoryAsync(IUserActionHistoryEntity userActionHistory);
    }
}
