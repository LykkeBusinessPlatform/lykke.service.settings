using System.Collections.Generic;
using System.Linq;
using AzureRepositories.KeyValue;
using Core.KeyValue;

namespace Web.Models
{
    public class KeyValueModel
    {
        private readonly List<string> _mustBeGeneratedTypes = new List<string>
        {
            KeyValueTypes.AzureTableStorage,
            KeyValueTypes.MongoDB,
            KeyValueTypes.RabbitMq,
            KeyValueTypes.Redis,
            KeyValueTypes.SqlDB,
        };

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
            IsJsonType = entry?.Types != null && (entry.Types.Contains(KeyValueTypes.Json) || entry.Types.Contains(KeyValueTypes.JsonArray));
            MustGenerateValue = entry?.Types != null && entry.Types.Any(t => _mustBeGeneratedTypes.Contains(t));
        }

        public static List<KeyValueModel> WithOld(IEnumerable<IKeyValueEntity> oridinEnt)
        {
            var list = oridinEnt.Select(oe => new KeyValueModel(oe as KeyValueEntity)).OrderBy(kvm => kvm.RowKey).ToList();

            return list;
        }
    }
}
