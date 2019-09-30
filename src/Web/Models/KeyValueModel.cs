using System.Collections.Generic;
using System.Linq;
using AzureRepositories.KeyValue;
using Core.KeyValue;

namespace Web.Models
{
    public class KeyValueModel
    {
        private const string _jsonType = "Json";
        private const string _jsonArrayType = "JsonArray";
        private const string _azureTableStorageMetadata = "AzureTableStorage";
        private const string _mongoDBMetadata = "MongoDB";
        private const string _rabbitMQMetadata = "RabbitMq";
        private const string _redisMetadata = "Redis";

        public KeyValueModel()
        {
        }

        public KeyValueModel(KeyValueEntity entry)
        {
            Key = entry.RowKey;
            ETag = entry.ETag;
            Value = entry.Value;
            Override = entry.Override;
            IsDuplicated = entry.IsDuplicated;
            UseNotTaggedValue = entry.UseNotTaggedValue;
            IsUsedInRepository = entry.RepositoryNames != null && entry.RepositoryNames.Length > 0;
            Types = entry.Types;
            RepositoryNames = entry.RepositoryNames;
            Tag = entry.Tag;
            EmptyValueType = entry.EmptyValueType;
            HasFullAccess = entry.HasFullAccess;
            IsJsonType = entry != null && entry.Types != null && (entry.Types.Contains(_jsonType) || entry.Types.Contains(_jsonArrayType));
            MustGenerateValue = entry != null && entry.Types != null && (entry.Types.Contains(_azureTableStorageMetadata) || entry.Types.Contains(_mongoDBMetadata) || entry.Types.Contains(_rabbitMQMetadata) || entry.Types.Contains(_redisMetadata));
        }

        public string RowKey => Key;
        public string Key { get; set; }
        public string ETag { get; set; }
        public string Value { get; set; }
        public bool? IsDuplicated { get; set; }
        public bool? UseNotTaggedValue { get; set; }
        public string[] Types { get; set; }
        public string[] RepositoryNames { get; set; }
        public OverrideValue[] Override { get; set; }
        public bool IsUsedInRepository { get; set; }
        public bool IsJsonType { get; set; }
        public bool MustGenerateValue { get; set; }
        public bool? HasFullAccess { get; set; }
        public string Tag { get; set; }
        public string EmptyValueType { get; set; }

        public static List<KeyValueModel> WithOld(IEnumerable<IKeyValueEntity> oridinEnt)
        {
            var list = oridinEnt.Select(oe => new KeyValueModel(oe as KeyValueEntity)).OrderBy(kvm => kvm.RowKey).ToList();

            return list;
        }
    }
}
