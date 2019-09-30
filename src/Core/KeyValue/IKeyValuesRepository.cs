using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.KeyValue
{
    public interface IKeyValuesRepository
    {
        Task<Dictionary<string, IKeyValueEntity>> GetAsync();
        Task<IEnumerable<IKeyValueEntity>> GetAsync(Func<IKeyValueEntity, bool> filter);
        Task<IEnumerable<IKeyValueEntity>> GetKeyValuesAsync();
        Task<IEnumerable<IKeyValueEntity>> GetKeyValuesAsync(Func<IKeyValueEntity, bool> filter, string repositoryId = null);
        Task<IKeyValueEntity> GetKeyValueAsync(string key);
        Task<Dictionary<string, IKeyValueEntity>> GetKeyValuesAsync(IEnumerable<string> keys);

        Task<bool> UpdateKeyValueAsync(IEnumerable<IKeyValueEntity> keyValueList);
        Task<bool> ReplaceKeyValueAsync(IEnumerable<IKeyValueEntity> keyValueList);
        Task RemoveNetworkOverridesAsync(string networkId);

        Task DeleteKeyValueWithHistoryAsync(string keyValueId, string description, string userName,
            string userIpAddress);

        Task DeleteKeyValuesWithHistoryAsync(string[] keyValueIds, string description, string userName, string userIpAddress);
    }
}
