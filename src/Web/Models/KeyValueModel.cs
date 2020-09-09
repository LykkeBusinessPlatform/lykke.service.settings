using System.Collections.Generic;
using System.Linq;
using Core.Entities;
using Core.KeyValue;
using Core.Models;

namespace Web.Models
{
    public class KeyValueModel : IKeyValueEntity
    {
        private readonly List<string> _mustBeGeneratedTypes = new List<string>
        {
            KeyValueTypes.AzureTableStorage,
            KeyValueTypes.MongoDB,
            KeyValueTypes.RabbitMq,
            KeyValueTypes.Redis,
            KeyValueTypes.SqlDB,
        };

        public string KeyValueId { get; set; }
        public string Value { get; set; }
        public bool? IsDuplicated { get; set; }
        public bool? UseNotTaggedValue { get; set; }
        public string[] Types { get; set; }
        public string[] RepositoryNames { get; set; }
        public string RepositoryId { get; set; }
        public OverrideValue[] Override { get; set; }
        public bool IsUsedInRepository { get; set; }
        public bool IsJsonType { get; set; }
        public bool MustGenerateValue { get; set; }
        public string Tag { get; set; }
        public string EmptyValueType { get; set; }

        public KeyValueModel()
        {
        }

        public KeyValueModel(IKeyValueEntity entry)
        {
            KeyValueId = entry.KeyValueId;
            Value = entry.Value;
            Override = entry.Override;
            IsDuplicated = entry.IsDuplicated;
            UseNotTaggedValue = entry.UseNotTaggedValue;
            IsUsedInRepository = entry.RepositoryNames != null && entry.RepositoryNames.Length > 0;
            Types = entry.Types;
            RepositoryNames = entry.RepositoryNames;
            Tag = entry.Tag;
            EmptyValueType = entry.EmptyValueType;
            IsJsonType = entry?.Types != null && (entry.Types.Contains(KeyValueTypes.Json) || entry.Types.Contains(KeyValueTypes.JsonArray));
            MustGenerateValue = entry?.Types != null && entry.Types.Any(t => _mustBeGeneratedTypes.Contains(t));
        }

        public static List<KeyValueModel> WithOld(IEnumerable<IKeyValueEntity> oridinEnt)
        {
            var list = oridinEnt.Select(oe => new KeyValueModel(oe)).OrderBy(kvm => kvm.KeyValueId).ToList();

            return list;
        }
    }
}
