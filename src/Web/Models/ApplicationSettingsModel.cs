using System;
using Core.Entities;

namespace Web.Models
{
    public class ApplicationSettingsModel : IApplicationSettingsEntity
    {
        public string SettingsId { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public string AzureClientId { get; set; }

        public string AzureRegionName { get; set; }

        public string AzureClientKey { get; set; }

        public string AzureTenantId { get; set; }

        public string AzureResourceGroupName { get; set; }

        public string AzureStorageName { get; set; }

        public string AzureKeyName { get; set; }

        public string AzureSubscriptionId { get; set; }

        public string AzureApiKey { get; set; }
        public string DefaultMongoDBConnStr { get; set; }
        public string DefaultRabbitMQConnStr { get; set; }
        public string DefaultRedisConnStr { get; set; }
    }
}
