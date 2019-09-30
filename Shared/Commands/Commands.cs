using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories.KeyValue;
using Core.ApplicationSettings;
using Core.Command;
using Core.KeyValue;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Newtonsoft.Json;

namespace Shared.Commands
{
    public class Commands : ICommand
    {
        private readonly IKeyValuesRepository _keyValuesRepository;
        private readonly IKeyValueHistoryRepository _keyValueHistoryRepository;
        private readonly IApplicationSettingsRepostiory _applicationSettingsRepostiory;

        public static string SetValueCommand => "-setvalue";
        public static string KeyValueCommand => "-keyvalue";
        public static string GenerateConnStringCommand => "-generateConnStr";
        public static string FromCommand => "-from";

        public const string GenerateAzureCommand = "-generateAzureConnStr";
        public const string GenerateMongoDBCommand = "-generateMongoDBConnStr";
        public const string GenerateRabbitMQCommand = "-generateRabbitMQConnStr";
        public const string GenerateRedisCommand = "-generateRedisConnStr";

        public const string AzureTableStorageMetadata = "AzureTableStorage";
        public const string MongoDBMetadata = "MongoDB";
        public const string RabbitMQMetadata = "RabbitMq";
        public const string RedisMetadata = "Redis";

        // created dictionary and stored metadata as key and command as value. this dicionary will help us to dynamically check metadatas and generate suitable command for them
        public static Dictionary<string, string> AllowedMetadatasToGenerate = new Dictionary<string, string>()
        {
            { AzureTableStorageMetadata, GenerateAzureCommand },
            { MongoDBMetadata, GenerateMongoDBCommand },
            { RabbitMQMetadata, GenerateRabbitMQCommand },
            { RedisMetadata, GenerateRedisCommand }
        };

        public Commands(
            IKeyValuesRepository keyValuesRepository,
            IKeyValueHistoryRepository keyValueHistoryRepository,
            IApplicationSettingsRepostiory applicationSettingsRepostiory)
        {
            _keyValuesRepository = keyValuesRepository ?? throw new ArgumentNullException(nameof(keyValuesRepository));
            _keyValueHistoryRepository = keyValueHistoryRepository ?? throw new ArgumentNullException(nameof(keyValueHistoryRepository));
            _applicationSettingsRepostiory = applicationSettingsRepostiory ?? throw new ArgumentNullException(nameof(applicationSettingsRepostiory));
        }

        public async Task SetValue(IKeyValueEntity entity, string fromService = "")
        {
            var keyValues = await _keyValuesRepository.GetKeyValuesAsync();
            var keyValue = keyValues.FirstOrDefault(x => x.RowKey == entity.RowKey);
            if (keyValue == null)
            {
                keyValue = new KeyValueEntity
                {
                    RowKey = entity.RowKey,
                    Value = entity.Value,
                    EmptyValueType = entity.EmptyValueType
                };
            }

            var oldValue = keyValue.Value;
            keyValue.Value = entity.Value;
            keyValue.EmptyValueType = entity.EmptyValueType;

            await UpdateKeyValue(keyValue, oldValue, keyValues, fromService);
        }

        public Task GetKeyValue(IKeyValueEntity entity, string from = "")
        {
            throw new NotImplementedException();
        }

        public async Task GenerateValue(IKeyValueEntity entity, string command, string from = "")
        {
            var keyValues = await _keyValuesRepository.GetKeyValuesAsync(x => x.RowKey == entity.RowKey);

            if (!keyValues.Any())
                return;

            var keyValue = keyValues.First();
            var oldValue = keyValue.Value;

            if (!string.IsNullOrWhiteSpace(keyValue.Value))
                return;

            if (keyValue.Types != null)
            {
                var types = keyValue.Types;

                // iterate through keyValueEntity types, check if allowedMetadatasToGenerate dictionary keys contains the specific type and assign command to "command" variable
                foreach (var item in types)
                {
                    var type = item.Trim();
                    if (AllowedMetadatasToGenerate.ContainsKey(type))
                    {
                        command = AllowedMetadatasToGenerate[type];
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(command) && string.IsNullOrEmpty(entity.Value))
            {
                var applicationSettings = await _applicationSettingsRepostiory.GetAsync();

                var connectionString = await GenerateConnectionString(command, applicationSettings);

                keyValue.Value = connectionString;
            }

            await UpdateKeyValue(keyValue, oldValue, keyValues, @from);
        }

        public static async Task<string> GenerateConnectionString(string command, IApplicationSettingsEntity applicationSettings)
        {
            var connectionString = "";
            if (applicationSettings == null)
                return connectionString;

            switch (command)
            {
                case GenerateAzureCommand:
                    var creds = new AzureCredentialsFactory().FromServicePrincipal(applicationSettings.AzureClientId, applicationSettings.AzureClientKey, applicationSettings.AzureTenantId, AzureEnvironment.AzureGlobalCloud);
                    var azure = Azure.Configure().WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                        .Authenticate(creds).WithSubscription(applicationSettings.AzureSubscriptionId);

                    //var storageAccount = azure.StorageAccounts.GetByResourceGroup(applicationSettings.AzureResourceGroupName, applicationSettings.AzureStorageName);
                    var newStorage = await azure.StorageAccounts.Define(applicationSettings.AzureStorageName)
                        .WithRegion(applicationSettings.AzureRegionName)
                        .WithExistingResourceGroup(applicationSettings.AzureResourceGroupName)
                        .CreateAsync();

                    var key = newStorage.RegenerateKey(applicationSettings.AzureKeyName);
                    connectionString = $"DefaultEndpointsProtocol=http;AccountName={newStorage.Name};AccountKey={key.FirstOrDefault()?.Value}";
                    break;
                case GenerateMongoDBCommand:
                    connectionString = applicationSettings.DefaultMongoDBConnStr;
                    break;
                case GenerateRabbitMQCommand:
                    connectionString = applicationSettings.DefaultRabbitMQConnStr;
                    break;
                case GenerateRedisCommand:
                    connectionString = applicationSettings.DefaultRedisConnStr;
                    break;
            }

            return connectionString;
        }

        private async Task UpdateKeyValue(
            IKeyValueEntity keyValueEntity,
            string oldValue,
            IEnumerable<IKeyValueEntity> keyValues,
            string fromService)
        {
            var duplicatedKeys = keyValues.Where(x => x.RowKey != keyValueEntity.RowKey && !string.IsNullOrEmpty(x.Value) && x.Value == keyValueEntity.Value).ToList();

            keyValueEntity.IsDuplicated = duplicatedKeys.Count > 0;

            var entitiesToUpload = new List<IKeyValueEntity>() { keyValueEntity };

            if (duplicatedKeys.Any())
            {
                var duplicationsToUpload = duplicatedKeys.Where(x => !x.IsDuplicated.HasValue || !x.IsDuplicated.Value);
                duplicationsToUpload.ToList().ForEach(item =>
                {
                    item.IsDuplicated = true;
                    entitiesToUpload.Add(item);
                });
            }

            var oldDuplications = keyValues.Where(x => x.RowKey != keyValueEntity.RowKey && x.Value == oldValue);
            if (oldDuplications.Count() == 1)
            {
                var oldDuplication = oldDuplications.First();
                oldDuplication.IsDuplicated = false;
                entitiesToUpload.Add(oldDuplication);
            }

            await _keyValuesRepository.UpdateKeyValueAsync(entitiesToUpload);
            string strObj = JsonConvert.SerializeObject(keyValues);
            await _keyValueHistoryRepository.SaveKeyValueHistoryAsync(
                keyValueEntity.RowKey,
                keyValueEntity.Value,
                strObj,
                "Recived settings data from " + fromService,
                "");
        }
    }
}
