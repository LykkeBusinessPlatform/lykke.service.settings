using System.Collections.Generic;
using Core.Entities;
using Core.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace AzureRepositories.KeyValue
{
    public class KeyValueEntity : TableEntity, IKeyValueEntity
    {
        private string _keyValueId;

        public static string GeneratePartitionKey() => "K";
        public static string GenerateRowKey(string key) => key;

        public string KeyValueId
        {
            get => _keyValueId ?? RowKey;
            set => _keyValueId = value;
        }
        public string Value { get; set; }
        public OverrideValue[] Override { get; set; }
        public string[] Types { get; set; }
        public bool? IsDuplicated { get; set; }
        public bool? UseNotTaggedValue { get; set; }
        public string[] RepositoryNames { get; set; }

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

            if (properties.TryGetValue(nameof(Value), out var val))
                Value = val.StringValue;

            if (properties.TryGetValue(nameof(KeyValueId), out var kvId))
                KeyValueId = kvId.StringValue;

            if (properties.TryGetValue(nameof(Types), out var types))
                Types = JsonConvert.DeserializeObject<string[]>(types.StringValue);

            if (properties.TryGetValue(nameof(IsDuplicated), out var isDuplicated))
                IsDuplicated = isDuplicated.BooleanValue;

            if (properties.TryGetValue(nameof(UseNotTaggedValue), out var useNotTaggedValue))
                UseNotTaggedValue = useNotTaggedValue.BooleanValue;

            if (properties.TryGetValue(nameof(RepositoryNames), out var repositoryIds))
                RepositoryNames = JsonConvert.DeserializeObject<string[]>(repositoryIds.StringValue);

            if (properties.TryGetValue(nameof(RepositoryId), out var repositoryId))
                RepositoryId = repositoryId.StringValue;

            if (properties.TryGetValue(nameof(Tag), out var tag))
                Tag = tag.StringValue;

            if (properties.TryGetValue(nameof(EmptyValueType), out var emptyValueType))
                EmptyValueType = emptyValueType.StringValue;
        }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var dict = new Dictionary<string, EntityProperty>
            {
                {nameof(Override), new EntityProperty(JsonConvert.SerializeObject(Override))},
                {nameof(KeyValueId), new EntityProperty(KeyValueId)},
                {nameof(Value), new EntityProperty(Value)},
                {nameof(Types), new EntityProperty(JsonConvert.SerializeObject(Types))},
                {nameof(IsDuplicated), new EntityProperty(IsDuplicated)},
                {nameof(UseNotTaggedValue), new EntityProperty(UseNotTaggedValue)},
                {nameof(RepositoryNames), new EntityProperty(JsonConvert.SerializeObject(RepositoryNames))},
                {nameof(RepositoryId), new EntityProperty(RepositoryId) },
                {nameof(Tag), new EntityProperty(Tag) },
                {nameof(EmptyValueType), new EntityProperty(EmptyValueType) }
            };

            return dict;
        }
    }
}
