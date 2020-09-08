using Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IRepositoriesUpdateHistoryRepository
    {
        Task<IRepositoryUpdateHistory> GetAsync(string initialCommit);
        Task<IEnumerable<IRepositoryUpdateHistory>> GetAllAsync();
        Task SaveRepositoryUpdateHistory(IRepositoryUpdateHistory entity);
        Task<IEnumerable<IRepositoryUpdateHistory>> GetAsync(Func<IRepositoryUpdateHistory, bool> filter);
        Task<IEnumerable<IRepositoryUpdateHistory>> GetAsyncByInitialCommit(string initialCommit);
        Task RemoveRepositoryUpdateHistoryAsync(string repositoryUpdateHistoryId);
        Task RemoveRepositoryUpdateHistoryAsync(IEnumerable<IRepositoryUpdateHistory> repositories);
    }
}
