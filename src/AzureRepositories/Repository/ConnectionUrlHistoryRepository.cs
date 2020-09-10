using AzureStorage;
using Core.Entities;
using Core.Repositories;
using Lykke.AzureStorage.Tables.Paging;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureRepositories.Repository
{
    public class ConnectionUrlHistoryRepository : IConnectionUrlHistoryRepository
    {
        private readonly INoSQLTableStorage<ConnectionUrlHistory> _tableStorage;

        private int? _totalCount;
        private Task _totalCountTask;

        public ConnectionUrlHistoryRepository(INoSQLTableStorage<ConnectionUrlHistory> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IConnectionUrlHistory> GetAsync(string connectionUrlHistoryId)
        {
            var pk = ConnectionUrlHistory.GeneratePartitionKey();
            var rk = ConnectionUrlHistory.GenerateRowKey(connectionUrlHistoryId);

            return await _tableStorage.GetDataAsync(pk, rk);
        }

        public async Task<(IEnumerable<IConnectionUrlHistory>, int)> GetPageAsync(int pageNum, int pageSize)
        {
            var pk = ConnectionUrlHistory.GeneratePartitionKey();
            var filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, pk);
            var query =  new TableQuery<ConnectionUrlHistory>().Where(filter);
            var pageItems = new List<ConnectionUrlHistory>();
            int batchCount = -1;
            int count = 0;
            int skip = (pageNum - 1) * pageSize;
            await _tableStorage.ExecuteAsync(
                query,
                batch => {
                    batchCount = batch.Count();
                    if (count + batchCount < skip)
                        return;
                    foreach(var item in batch)
                    {
                        if (count < skip)
                        {
                            ++count;
                            continue;
                        }
                        if (pageItems.Count == pageSize)
                            return;
                        pageItems.Add(item);
                    }
                    count += batchCount;
                },
                () => batchCount > 0 && pageItems.Count < pageSize);

            var totalCount = 10 * pageSize;
            if (_totalCount.HasValue)
                totalCount = _totalCount.Value;
            else if (_totalCountTask == null)
                Task.Run(async () =>
                {
                    _totalCountTask = CalculateTotalCountAsync();
                    await _totalCountTask;
                });
            return (pageItems, totalCount);
        }

        public async Task<IEnumerable<IConnectionUrlHistory>> GetAllAsync(Func<IConnectionUrlHistory, bool> filter)
        {
            var pk = ConnectionUrlHistory.GeneratePartitionKey();
            var list = await _tableStorage.GetDataAsync(pk, filter);
            return list;
        }

        public async Task SaveConnectionUrlHistory(IConnectionUrlHistory entity)
        {
            if (!(entity is ConnectionUrlHistory cuh))
            {
                cuh = (ConnectionUrlHistory)await GetAsync(entity.RowKey) ?? new ConnectionUrlHistory();

                cuh.ETag = entity.ETag;
                cuh.Ip = entity.Ip;
                cuh.UserAgent = entity.UserAgent;
            }
            cuh.PartitionKey = ConnectionUrlHistory.GeneratePartitionKey();
            cuh.RowKey = entity.RowKey;
            await _tableStorage.InsertOrMergeAsync(cuh);
        }

        private async Task CalculateTotalCountAsync()
        {
            if (_totalCountTask != null)
                return;
            try
            {
                var pk = ConnectionUrlHistory.GeneratePartitionKey();
                int totalCount = 0;
                await _tableStorage.GetDataByChunksAsync(pk, c =>
                {
                    totalCount += c.Count();
                });
                _totalCount = totalCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                _totalCountTask = null;
            }
        }
    }
}
