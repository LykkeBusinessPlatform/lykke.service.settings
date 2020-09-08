using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.Entities;
using Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.KeyValue
{
    public class KeyValuesRepository : ISecretKeyValuesRepository
    {
        private readonly INoSQLTableStorage<KeyValueEntity> _tableStorage;
        private readonly IKeyValueHistoryRepository _history;

        public KeyValuesRepository(INoSQLTableStorage<KeyValueEntity> tableStorage, IKeyValueHistoryRepository history)
        {
            _tableStorage = tableStorage;
            _history = history;
        }
        public async Task<Dictionary<string, IKeyValueEntity>> GetAsync()
        {
            var entries = await GetKeyValuesAsync();
            return entries.ToDictionary(itm => itm.RowKey, itm => itm);
        }

        public async Task<IKeyValueEntity> GetTopRecordAsync()
        {
            var pk = KeyValueEntity.GeneratePartitionKey();
            var result = await _tableStorage.GetTopRecordAsync(pk);
            return result;
        }

        public async Task<IEnumerable<IKeyValueEntity>> GetAsync(Func<IKeyValueEntity, bool> filter)
        {
            var pk = KeyValueEntity.GeneratePartitionKey();
            var list = await _tableStorage.GetDataAsync(pk, filter);
            return list;
        }

        public async Task<IEnumerable<IKeyValueEntity>> GetKeyValuesAsync()
        {
            var pk = KeyValueEntity.GeneratePartitionKey();
            return await _tableStorage.GetDataAsync(pk);
        }

        public async Task<IEnumerable<IKeyValueEntity>> GetKeyValuesAsync(Func<IKeyValueEntity, bool> filter, string repositoryId = null)
        {
            string queryText = TableQuery.GenerateFilterCondition(nameof(KeyValueEntity.PartitionKey), QueryComparisons.Equal, KeyValueEntity.GeneratePartitionKey());
            if (!string.IsNullOrWhiteSpace(repositoryId))
            {
                string repositoryFilter = TableQuery.GenerateFilterCondition(nameof(KeyValueEntity.RepositoryId), QueryComparisons.Equal, repositoryId);
                queryText = TableQuery.CombineFilters(queryText, TableOperators.And, repositoryFilter);
            }
            var query = new TableQuery<KeyValueEntity>().Where(queryText);
            return await _tableStorage.WhereAsync(query, filter);
        }

        public async Task<IKeyValueEntity> GetKeyValueAsync(string key)
        {
            return await _tableStorage.GetDataAsync(KeyValueEntity.GeneratePartitionKey(), KeyValueEntity.GenerateRowKey(key));
        }

        public async Task<Dictionary<string, IKeyValueEntity>> GetKeyValuesAsync(IEnumerable<string> keys)
        {
            var items = await _tableStorage.GetDataAsync(KeyValueEntity.GeneratePartitionKey(), keys.Select(KeyValueEntity.GenerateRowKey));
            return items.ToDictionary(i => i.RowKey, i => (IKeyValueEntity)i);
        }

        public async Task<bool> UpdateKeyValueAsync(IEnumerable<IKeyValueEntity> keyValueList)
        {
            try
            {
                await _tableStorage.InsertOrMergeBatchAsync(keyValueList.Cast<KeyValueEntity>());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }

            return true;
        }

        public async Task<bool> ReplaceKeyValueAsync(IEnumerable<IKeyValueEntity> keyValueList)
        {
            try
            {
                await _tableStorage.InsertOrReplaceBatchAsync(keyValueList.Cast<KeyValueEntity>());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }

            return true;
        }

        public async Task RemoveNetworkOverridesAsync(string networkId)
        {
            IEnumerable<IKeyValueEntity> keyValues = (await GetKeyValuesAsync()).Where(item => item.Override != null && item.Override.Any(o => o.NetworkId == networkId));

            var keyValuesToUpdate = new List<IKeyValueEntity>();

            foreach (IKeyValueEntity keyValue in keyValues)
            {
                var list = keyValue.Override.ToList();
                var overrideValue = list.FirstOrDefault(item => item.NetworkId == networkId);

                if (overrideValue != null)
                {
                    list.Remove(overrideValue);
                    keyValue.Override = list.ToArray();
                    keyValuesToUpdate.Add(keyValue);
                }
            }

            await UpdateKeyValueAsync(keyValuesToUpdate);
        }

        public async Task DeleteKeyValueWithHistoryAsync(string keyValueId, string description, string userName, string userIpAddress)
        {
            var kvItem = await _tableStorage.GetDataAsync(KeyValueEntity.GeneratePartitionKey(), keyValueId);
            if (kvItem != null)
            {
                await _tableStorage.DeleteAsync(kvItem);
                await _history.DeleteKeyValueHistoryAsync(keyValueId, description, userName, userIpAddress);
            }
        }
    }
}
