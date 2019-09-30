using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureRepositories.Extensions;
using AzureStorage;
using Core.KeyValue;
using Newtonsoft.Json;
using IBlobStorage = Core.Blob.IBlobStorage;

namespace AzureRepositories.KeyValue
{
    public class KeyValueHistoryRepository : IKeyValueHistoryRepository
    {
        private readonly INoSQLTableStorage<KeyValueHistory> _tableStorage;
        private readonly IBlobStorage _blobStorage;
        private readonly string _container;

        public KeyValueHistoryRepository(
            INoSQLTableStorage<KeyValueHistory> tableStorage,
            IBlobStorage blobStorage,
            string container)
        {
            _tableStorage = tableStorage;
            _blobStorage = blobStorage;
            _container = container;
        }

        public async Task SaveKeyValueOverrideHistoryAsync(
            string keyValueId,
            string newOverride,
            string keyValues,
            string userName,
            string userIpAddress)
        {
            var th = new KeyValueHistory
            {
                KeyValueId = keyValueId,
                NewOverride = newOverride,
                PartitionKey = keyValueId,
                RowKey = DateTime.UtcNow.StorageString(),
                UserName = userName,
                UserIpAddress = userIpAddress
            };

            th.KeyValuesSnapshot = $"{th.UserName}_{th.RowKey}_{th.UserIpAddress}";

            await _blobStorage.SaveBlobAsync(_container, th.KeyValuesSnapshot, Encoding.UTF8.GetBytes(keyValues));

            await _tableStorage.InsertOrMergeAsync(th);
        }

        public async Task SaveKeyValueHistoryAsync(
            string keyValueId,
            string newValue,
            string keyValues,
            string userName,
            string userIpAddress)
        {
            var th = new KeyValueHistory
            {
                KeyValueId = keyValueId,
                NewValue = newValue,
                PartitionKey = keyValueId,
                RowKey = DateTime.UtcNow.StorageString(),
                UserName = userName,
                UserIpAddress = userIpAddress
            };

            th.KeyValuesSnapshot = $"{th.UserName}_{th.RowKey}_{th.UserIpAddress}";

            await _blobStorage.SaveBlobAsync(_container, th.KeyValuesSnapshot, Encoding.UTF8.GetBytes(keyValues));

            await _tableStorage.InsertOrMergeAsync(th);
        }

        public Task SaveKeyValuesHistoryAsync(
            IEnumerable<IKeyValueEntity> keyValues,
            string userName,
            string userIpAddress)
        {
            var timestamp = DateTime.UtcNow.StorageString();
            var snapshotName = $"{userName}_{timestamp}_{userIpAddress}";
            string valuesSnapshot = JsonConvert.SerializeObject(keyValues);
            var snapshotBytes = Encoding.UTF8.GetBytes(valuesSnapshot);
            //Don't await it, cause it's too heavy and doesn't affect subsequent operations.
            Task.Run(async () => await _blobStorage.SaveBlobAsync(_container, snapshotName, snapshotBytes));

            return Task.WhenAll(keyValues.Select(k =>
                _tableStorage.InsertOrMergeAsync(new KeyValueHistory
                {
                    PartitionKey = k.RowKey,
                    RowKey = timestamp,
                    KeyValueId = k.RowKey,
                    NewValue = k.Value,
                    UserName = userName,
                    UserIpAddress = userIpAddress,
                    KeyValuesSnapshot = snapshotName,
                })));
        }

        public async Task DeleteKeyValueHistoryAsync(string keyValueId, string description, string userName, string userIpAddress)
        {
            var th = new KeyValueHistory
            {
                KeyValueId = keyValueId,
                PartitionKey = keyValueId,
                RowKey = DateTime.UtcNow.StorageString(),
                UserName = userName,
                UserIpAddress = userIpAddress
            };

            th.KeyValuesSnapshot = $"{th.UserName}_{th.RowKey}_{th.UserIpAddress}";

            await _blobStorage.SaveBlobAsync(_container, th.KeyValuesSnapshot, Encoding.UTF8.GetBytes(description));

            await _tableStorage.InsertOrMergeAsync(th);
        }

        public async Task<List<IKeyValueHistory>> GetHistoryByKeyValueAsync(string keyValueId)
        {
            if (string.IsNullOrEmpty(keyValueId))
            {
                return new List<IKeyValueHistory>();
            }

            var hist = await _tableStorage.GetDataAsync(keyValueId);
            var history = from h in hist
                where keyValueId.Equals(h.KeyValueId)
                orderby h.Timestamp descending
                select (IKeyValueHistory)h;

            return history.ToList();
        }

        public async Task<List<IKeyValueHistory>> GetAllAsync()
        {
            var hist = await _tableStorage.GetDataAsync();
            return hist.Select(kvh=>(IKeyValueHistory)kvh).ToList();
        }

        public async Task<Dictionary<string, byte[]>> GetAllBlobAsync()
        {
            var keys = await _blobStorage.GetListOfBlobsAsync(_container);
            var result = new Dictionary<string, byte[]>();
            foreach (var k in keys)
            {
                var ks = k.Split(@"/\".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (ks.Length == 0)
                {
                    continue;
                }
                result.Add(ks[ks.Length - 1], (await _blobStorage.GetAsync(_container, ks[ks.Length-1])).AsBytes());
            }

            return result;
        }
    }
}
