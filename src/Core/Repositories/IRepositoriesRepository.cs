using Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IRepositoriesRepository
    {
        Task<IRepository> GetAsync(string repositoryId);
        Task<IEnumerable<IRepository>> GetAllAsync();
        Task RemoveRepositoryAsync(string repositoryId);
        Task SaveRepositoryAsync(IRepository repository);
        Task<IEnumerable<IRepository>> GetAsync(Func<IRepository, bool> filter);
    }
}
