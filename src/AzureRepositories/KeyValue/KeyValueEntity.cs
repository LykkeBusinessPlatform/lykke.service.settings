using System.Collections.Generic;
using Core.KeyValue;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace AzureRepositories.KeyValue
{
    public class KeyValueEntity : TableEntity, IKeyValueEntity
    {
        public static string GeneratePartitionKey() => "K";
        public static string GenerateRowKey(string key) => key;

        public string Value { get; set; }
        public OverrideValue[] Override { get; set; }
        public string[] Types { get; set; }
        public bool? IsDuplicated { get; set; }
        public bool? UseNotTaggedValue { get; set; }
        public string[] RepositoryNames { get; set; }
        // TODO: should I place this variable here?
        public bool? HasFullAccess { get; set; }

        public string RepositoryId { get; set; }

        public string Tag { get; set; }

        public string EmptyValueType { get; set; }

        public KeyValueEntity()
        {
            PartitionKey = GeneratePartitionKey();
        }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            if (properties.TryGetValue("Override", out var property))
            {
                var json = property.StringValue;
                if (!string.IsNullOrEmpty(json))
                {
                    Override = JsonConvert.DeserializeObject<OverrideValue[]>(json);
                }
            }

            if (properties.TryGetValue("Value", out var val))
            {
                Value = val.StringValue;
            }

            if (properties.TryGetValue("Types", out var types))
            {
                var typesJson = types.StringValue;
                Types = JsonConvert.DeserializeObject<string[]>(typesJson);
            }

            if (properties.TryGetValue("IsDuplicated", out var isDuplicated))
            {
                IsDuplicated = isDuplicated.BooleanValue;
            }

            if (properties.TryGetValue("UseNotTaggedValue", out var useNotTaggedValue))
            {
                UseNotTaggedValue = useNotTaggedValue.BooleanValue;
            }

            if (properties.TryGetValue("RepositoryNames", out var repositoryIds))
            {
                var repositoryIdsJson = repositoryIds.StringValue;
                RepositoryNames = JsonConvert.DeserializeObject<string[]>(repositoryIdsJson);
            }

            if(properties.TryGetValue("RepositoryId", out var repositoryId))
            {
                RepositoryId = repositoryId.StringValue;
            }

            if(properties.TryGetValue("Tag", out var tag))
            {
                Tag = tag.StringValue;
            }

            if(properties.TryGetValue("EmptyValueType", out var emptyValueType))
            {
                EmptyValueType = emptyValueType.StringValue;
            }
        }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var dict = new Dictionary<string, EntityProperty>
            {
                {"Override", new EntityProperty(JsonConvert.SerializeObject(Override))},
                {"Value", new EntityProperty(Value)},
                {"Types", new EntityProperty(JsonConvert.SerializeObject(Types))},
                {"IsDuplicated", new EntityProperty(IsDuplicated)},
                {"UseNotTaggedValue", new EntityProperty(UseNotTaggedValue)},
                {"RepositoryNames", new EntityProperty(JsonConvert.SerializeObject(RepositoryNames))},
                {"RepositoryId", new EntityProperty(RepositoryId) },
                {"Tag", new EntityProperty(Tag) },
                {"EmptyValueType", new EntityProperty(EmptyValueType) }
            };

            return dict;
        }
    }
}
