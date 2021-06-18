using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.Repository;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Repository
{
    public class RepositoriesRepository : IRepositoriesRepository
    {
        private readonly INoSQLTableStorage<RepositoryEntity> _tableStorage;

        public RepositoriesRepository(INoSQLTableStorage<RepositoryEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IRepository> GetAsync(string repositoryId)
        {
            var pk = RepositoryEntity.GeneratePartitionKey();
            var rk = RepositoryEntity.GenerateRowKey(repositoryId);

            return await _tableStorage.GetDataAsync(pk, rk);
        }

        public async Task<bool> ExistsWithNameAsync(string repositoryName, string tag)
        {
            string queryText = TableQuery.GenerateFilterCondition(nameof(RepositoryEntity.PartitionKey), QueryComparisons.Equal, RepositoryEntity.GeneratePartitionKey());
            string repositoryFilter = TableQuery.GenerateFilterCondition(nameof(RepositoryEntity.Name), QueryComparisons.Equal, repositoryName);
            queryText = TableQuery.CombineFilters(queryText, TableOperators.And, repositoryFilter);
            var query = new TableQuery<RepositoryEntity>().Where(queryText);

            var records = await _tableStorage.WhereAsync(query, i => i.Tag == tag);
            return records.Any();
        }

        public async Task<IEnumerable<IRepository>> GetAsync(Func<IRepository, bool> filter)
        {
            var pk = RepositoryEntity.GeneratePartitionKey();
            var list = await _tableStorage.GetDataAsync(pk, filter:filter);
            return list as IEnumerable<IRepository>;
        }

        public async Task<IEnumerable<IRepository>> GetAllAsync()
        {
            var pk = RepositoryEntity.GeneratePartitionKey();
            return await _tableStorage.GetDataAsync(pk);
        }

        public async Task RemoveRepositoryAsync(string repositoryId)
        {
            var pk = RepositoryEntity.GeneratePartitionKey();
            await _tableStorage.DeleteAsync(pk, repositoryId);
        }

        public async Task SaveRepositoryAsync(IRepository repository)
        {
            if (!(repository is RepositoryEntity rs))
            {
                rs = (RepositoryEntity)await GetAsync(repository.RowKey) ?? new RepositoryEntity();

                rs.ETag = repository.ETag;
                rs.Name = repository.Name;
                rs.GitUrl = repository.GitUrl;
                rs.Branch = repository.Branch;
                rs.FileName = repository.FileName;
                rs.UserName = repository.UserName;
                rs.ConnectionUrl = repository.ConnectionUrl;
                rs.UseManualSettings = repository.UseManualSettings;
                rs.Tag = repository.Tag;
            }
            rs.PartitionKey = RepositoryEntity.GeneratePartitionKey();
            rs.RowKey = repository.RowKey;
            await _tableStorage.InsertOrMergeAsync(rs);
        }
    }
}
