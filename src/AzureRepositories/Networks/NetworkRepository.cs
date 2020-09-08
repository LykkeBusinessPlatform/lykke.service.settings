using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.Entities;
using Core.Models;
using Core.Repositories;

namespace AzureRepositories.Networks
{
    public class NetworkRepository : INetworkRepository
    {
        private readonly INoSQLTableStorage<NetworkEntity> _tableStorage;

        public NetworkRepository(INoSQLTableStorage<NetworkEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<Network[]> GetAllAsync()
        {
            return (await _tableStorage.GetDataAsync()).Select(NetworkEntity.ToDomain).ToArray();
        }

        public async Task<Network> GetByIpAsync(string ip)
        {
            var networks = await GetAllAsync();

            return networks.FirstOrDefault(item => item.Ips.Any(ip.StartsWith));
        }

        public async Task<bool> NetworkExistsAsync(string id)
        {
            var entity = await _tableStorage.GetDataAsync(NetworkEntity.GeneratePartitionKey(),
                NetworkEntity.GenerateRowKey(id));

            return entity != null;
        }

        public async Task AddAsync(INetwork network)
        {
            var existing = (await _tableStorage.GetDataAsync(x => x.Name == network.Name)).FirstOrDefault();

            if (existing != null)
            {
                await UpdateIp(existing.Id, network.Ip);
            }
            else
            {
                var entity = NetworkEntity.Create(network);
                await _tableStorage.TryInsertAsync(entity);
            }
        }
        
        public async Task UpdateAsync(INetwork network)
        {
            await UpdateIp(network.Id, network.Ip);
        }

        public async Task DeleteAsync(string id)
        {
            await _tableStorage.DeleteIfExistAsync(NetworkEntity.GeneratePartitionKey(),
                NetworkEntity.GenerateRowKey(id));
        }

        private async Task UpdateIp(string id, string ip)
        {
            await _tableStorage.MergeAsync(NetworkEntity.GeneratePartitionKey(), NetworkEntity.GenerateRowKey(id),
                entity =>
                {
                    entity.Ip = ip;
                    return entity;
                });
        }
    }
}
