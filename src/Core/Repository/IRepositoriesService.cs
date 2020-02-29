using Core.KeyValue;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repository
{
    public interface IRepositoriesService
    {
        Task<RepositoriesServiceModel> CreateRepositoryAsync(
            IRepository repository,
            string userName,
            string userIp,
            string userEmail,
            bool isProduction);

        Task<RepositoriesServiceModel> UpdateRepositoryAsync(
            IRepository repository,
            string userName,
            string userIp,
            string userEmail,
            bool isProduction,
            string search = null);

        Task<string> GetFileData(string file);

        Task AddToHistoryRepository(
            IRepository repository,
            string settingsJson,
            string lastCommit = "",
            bool isManual = false,
            string userName = "",
            string userIp = "");

        Task<List<IRepository>> GetAllRepositories();

        Task<RepositoriesServiceModel> GetPaginatedRepositories(string search = "", int? page = 1);

        Task<bool> SaveKeyValuesAsync(IEnumerable<IKeyValueEntity> keyValues, string userEmail, string userIp, bool isProduction);
    }
}
