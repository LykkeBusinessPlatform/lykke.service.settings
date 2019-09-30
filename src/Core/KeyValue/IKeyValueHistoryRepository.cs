using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.KeyValue
{
    public interface IKeyValueHistoryRepository
    {
        Task SaveKeyValueHistoryAsync(
            string keyValueId,
            string newValue,
            string keyValues,
            string userName,
            string userIpAddress);

        Task SaveKeyValuesHistoryAsync(
            IEnumerable<IKeyValueEntity> keyValues,
            string userName,
            string userIpAddress);

        Task SaveKeyValueOverrideHistoryAsync(
            string keyValueId,
            string newOverride,
            string keyValues,
            string userName,
            string userIpAddress);

        Task DeleteKeyValueHistoryAsync(
            string keyValueId,
            string description,
            string userName,
            string userIpAddress);

        Task<List<IKeyValueHistory>> GetHistoryByKeyValueAsync(string keyValueId);

        Task<List<IKeyValueHistory>> GetAllAsync();

        Task<Dictionary<string, byte[]>> GetAllBlobAsync();
    }
}
