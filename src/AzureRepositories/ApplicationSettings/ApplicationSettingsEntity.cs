using System;
using System.Collections.Generic;
using System.Text;
using Core.ApplicationSettings;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.ApplicationSettings
{
    public class ApplicationSettingsEntity:TableEntity, IApplicationSettingsEntity
    {
        public static string GeneratePartitionKey() => "AS";
        public static string GenerateRowKey(string applicationSettingsId) => applicationSettingsId;

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
        public string DefaultRedisConnStr  { get; set; }
        public string DefaultRabbitMQConnStr  { get; set; }
    }
}
