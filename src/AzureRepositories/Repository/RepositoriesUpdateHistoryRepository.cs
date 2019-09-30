using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using Core.Repository;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Repository
{
    public class RepositoriesUpdateHistoryRepository : IRepositoriesUpdateHistoryRepository
    {
        private readonly INoSQLTableStorage<RepositoryUpdateHistory> _tableStorage;

        public RepositoriesUpdateHistoryRepository(INoSQLTableStorage<RepositoryUpdateHistory> tableStorage)
        {
            _tableStorage = tableStorage;
        }
        
        public async Task<IRepositoryUpdateHistory> GetAsync(string repositoryUpdateHistoryId)
        {
            var pk = RepositoryUpdateHistory.GeneratePartitionKey();
            var rk = RepositoryUpdateHistory.GenerateRowKey(repositoryUpdateHistoryId);

            return await _tableStorage.GetDataAsync(pk, rk);
        }

        public async Task<IEnumerable<IRepositoryUpdateHistory>> GetAllAsync()
        {
            var pk = RepositoryUpdateHistory.GeneratePartitionKey();
            return await _tableStorage.GetDataAsync(pk);
        }

        public async Task RemoveRepositoryUpdateHistoryAsync(string repositoryUpdateHistoryId)
        {
            var pk = RepositoryUpdateHistory.GeneratePartitionKey();
            var rk = RepositoryUpdateHistory.GenerateRowKey(repositoryUpdateHistoryId);
            await _tableStorage.DeleteAsync(pk, rk);
        }
        
        public async Task RemoveRepositoryUpdateHistoryAsync(IEnumerable<IRepositoryUpdateHistory> repositories)
        {
            await _tableStorage.DeleteAsync((IEnumerable<RepositoryUpdateHistory>)repositories);
        }

        public async Task SaveRepositoryUpdateHistory(IRepositoryUpdateHistory entity)
        {
            if (!(entity is RepositoryUpdateHistory ruh))
            {
                ruh = (RepositoryUpdateHistory)await GetAsync(entity.RowKey) ?? new RepositoryUpdateHistory();

                ruh.ETag = entity.ETag;
                ruh.InitialCommit = entity.InitialCommit;
                ruh.User = entity.User;
                ruh.Branch = entity.Branch;
                ruh.IsManual = entity.IsManual;
            }
            ruh.PartitionKey = RepositoryUpdateHistory.GeneratePartitionKey();
            ruh.RowKey = entity.RowKey;
            await _tableStorage.InsertOrMergeAsync(ruh);
        }

        public async Task<IEnumerable<IRepositoryUpdateHistory>> GetAsync(Func<IRepositoryUpdateHistory, bool> filter)
        {
            var pk = RepositoryUpdateHistory.GeneratePartitionKey();
            var list = await _tableStorage.GetDataAsync(pk, filter);
            return list;
        }

        public async Task<IEnumerable<IRepositoryUpdateHistory>> GetAsyncByInitialCommit(string initialCommit)
        {
            string partitionFilter = TableQuery.GenerateFilterCondition(nameof(RepositoryUpdateHistory.PartitionKey), QueryComparisons.Equal, RepositoryUpdateHistory.GeneratePartitionKey());
            string repositoryFilter = TableQuery.GenerateFilterCondition(nameof(RepositoryUpdateHistory.InitialCommit), QueryComparisons.Equal, initialCommit);
            string queryText = TableQuery.CombineFilters(partitionFilter, TableOperators.And, repositoryFilter);
            var query = new TableQuery<RepositoryUpdateHistory>().Where(queryText);
            return await _tableStorage.WhereAsync(query);
        }
    }
}
