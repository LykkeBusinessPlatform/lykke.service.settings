using Core.Entities;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IUserActionHistoryRepository
    {
        Task SaveUserActionHistoryAsync(IUserActionHistoryEntity userActionHistory);
    }
}
