using AzureStorage;
using Core.Entities;
using Core.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzureRepositories.Repository
{
    public class ConnectionUrlHistoryRepository : IConnectionUrlHistoryRepository
    {
        private readonly INoSQLTableStorage<ConnectionUrlHistory> _tableStorage;

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

        public async Task<IEnumerable<IConnectionUrlHistory>> GetAllAsync()
        {
            var pk = ConnectionUrlHistory.GeneratePartitionKey();
            return await _tableStorage.GetDataAsync(pk);
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
    }
}
