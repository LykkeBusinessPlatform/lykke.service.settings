using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using Core.Entities;
using Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Repository
{
    public class RepositoriesUpdateHistoryRepository : IRepositoriesUpdateHistoryRepository
    {
        private readonly INoSQLTableStorage<RepositoryUpdateHistoryEntity> _tableStorage;

        public RepositoriesUpdateHistoryRepository(INoSQLTableStorage<RepositoryUpdateHistoryEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }
        
        public async Task<IRepositoryUpdateHistory> GetAsync(string repositoryUpdateHistoryId)
        {
            var pk = RepositoryUpdateHistoryEntity.GeneratePartitionKey();
            var rk = RepositoryUpdateHistoryEntity.GenerateRowKey(repositoryUpdateHistoryId);

            return await _tableStorage.GetDataAsync(pk, rk);
        }

        public async Task RemoveRepositoryUpdateHistoryAsync(string repositoryUpdateHistoryId)
        {
            var pk = RepositoryUpdateHistoryEntity.GeneratePartitionKey();
            var rk = RepositoryUpdateHistoryEntity.GenerateRowKey(repositoryUpdateHistoryId);
            await _tableStorage.DeleteAsync(pk, rk);
        }
        
        public async Task RemoveRepositoryUpdateHistoryAsync(IEnumerable<IRepositoryUpdateHistory> repositories)
        {
            await _tableStorage.DeleteAsync((IEnumerable<RepositoryUpdateHistoryEntity>)repositories);
        }

        public async Task SaveRepositoryUpdateHistory(IRepositoryUpdateHistory entity)
        {
            if (entity is RepositoryUpdateHistoryEntity ruh)
            {
                if (ruh.CreatedAt == null)
                    ruh.CreatedAt = DateTimeOffset.UtcNow;
                ruh.PartitionKey = RepositoryUpdateHistoryEntity.GeneratePartitionKey();
                ruh.RowKey = entity.RepositoryId;
            }
            else
            {
                var pk = RepositoryUpdateHistoryEntity.GeneratePartitionKey();
                var rk = RepositoryUpdateHistoryEntity.GenerateRowKey(entity.RepositoryId);
                ruh = await _tableStorage.GetDataAsync(pk, rk) ?? new RepositoryUpdateHistoryEntity();

                ruh.InitialCommit = entity.InitialCommit;
                ruh.User = entity.User;
                ruh.Branch = entity.Branch;
                ruh.IsManual = entity.IsManual;
                ruh.CreatedAt = DateTime.UtcNow;
            }

            await _tableStorage.InsertOrMergeAsync(ruh);
        }

        public async Task<IEnumerable<IRepositoryUpdateHistory>> GetAsyncByInitialCommit(string initialCommit)
        {
            string partitionFilter = TableQuery.GenerateFilterCondition(nameof(RepositoryUpdateHistoryEntity.PartitionKey), QueryComparisons.Equal, RepositoryUpdateHistoryEntity.GeneratePartitionKey());
            string repositoryFilter = TableQuery.GenerateFilterCondition(nameof(RepositoryUpdateHistoryEntity.InitialCommit), QueryComparisons.Equal, initialCommit);
            string queryText = TableQuery.CombineFilters(partitionFilter, TableOperators.And, repositoryFilter);
            var query = new TableQuery<RepositoryUpdateHistoryEntity>().Where(queryText);
            return await _tableStorage.WhereAsync(query);
        }
    }
}
