namespace Core.Entities
{
    public interface IApplicationSettingsEntity : IEntity
    {
        string AzureClientId { get; set; }

        string AzureRegionName { get; set; }

        string AzureClientKey { get; set; }

        string AzureTenantId { get; set; }

        string AzureResourceGroupName { get; set; }

        string AzureStorageName { get; set; }

        string AzureKeyName { get; set; }

        string AzureSubscriptionId { get; set; }

        string AzureApiKey { get; set; }
        string DefaultMongoDBConnStr { get; set; }
        string DefaultRedisConnStr { get; set; }
        string DefaultRabbitMQConnStr { get; set; }
    }
}
