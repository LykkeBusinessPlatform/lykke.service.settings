using Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IRepositoriesUpdateHistoryRepository
    {
        Task<IRepositoryUpdateHistory> GetAsync(string initialCommit);
        Task SaveRepositoryUpdateHistory(IRepositoryUpdateHistory entity);
        Task<IEnumerable<IRepositoryUpdateHistory>> GetAsyncByInitialCommit(string initialCommit);
        Task RemoveRepositoryUpdateHistoryAsync(string repositoryUpdateHistoryId);
        Task RemoveRepositoryUpdateHistoryAsync(IEnumerable<IRepositoryUpdateHistory> repositories);
    }
}
