using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.Entities;
using Core.Repositories;

namespace AzureRepositories.ApplicationSettings
{
    public class ApplicationSettingsRepository : IApplicationSettingsRepostiory
    {
        private readonly INoSQLTableStorage<ApplicationSettingsEntity> _tableStorage;

        public ApplicationSettingsRepository(INoSQLTableStorage<ApplicationSettingsEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IApplicationSettingsEntity> GetAsync()
        {
            var pk = ApplicationSettingsEntity.GeneratePartitionKey();

            var list = await _tableStorage.GetDataAsync(pk);
            var applicationSettingsEntities = list as ApplicationSettingsEntity[] ?? list.ToArray();
            return applicationSettingsEntities.Any() ? applicationSettingsEntities.First() : new ApplicationSettingsEntity();
        }

        public async Task SaveApplicationSettings(IApplicationSettingsEntity entity)
        {
            if (!(entity is ApplicationSettingsEntity se))
            {
                se = new ApplicationSettingsEntity
                {
                    ETag = entity.ETag,
                    AzureClientId = entity.AzureClientId,
                    AzureRegionName = entity.AzureRegionName,
                    AzureClientKey = entity.AzureClientKey,
                    AzureTenantId = entity.AzureTenantId,
                    AzureResourceGroupName = entity.AzureResourceGroupName,
                    AzureStorageName = entity.AzureStorageName,
                    AzureKeyName = entity.AzureKeyName,
                    AzureSubscriptionId = entity.AzureSubscriptionId,
                    AzureApiKey = entity.AzureApiKey,
                    DefaultMongoDBConnStr = entity.DefaultMongoDBConnStr,
                    DefaultRabbitMQConnStr = entity.DefaultRabbitMQConnStr,
                    DefaultRedisConnStr = entity.DefaultRedisConnStr
                };
            }
            se.PartitionKey = ApplicationSettingsEntity.GeneratePartitionKey();
            se.RowKey = entity.RowKey;
            await _tableStorage.InsertOrMergeAsync(se);
        }
    }
}
