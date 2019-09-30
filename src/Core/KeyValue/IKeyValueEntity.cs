using System;

namespace Core.KeyValue
{
    public interface IKeyValueEntity : IEntity
    {
        string Value { get; set; }
        OverrideValue[] Override { get; set; }
        string[] Types { get; set; }
        bool? IsDuplicated { get; set; }
        bool? UseNotTaggedValue { get; set; }
        string[] RepositoryNames { get; set; }
        string RepositoryId { get; set; }
        string Tag { get; set; }
        string EmptyValueType { get; set; }
    }

    public class KeyValue : IKeyValueEntity
    {
        public string RowKey { get; set; }
        public string ETag { get; set; }
        public string Value { get; set; }
        public OverrideValue[] Override { get; set; } = Array.Empty<OverrideValue>();
        public string[] Types { get; set; }
        public bool? IsDuplicated { get; set; }
        public bool? UseNotTaggedValue { get; set; }
        public string[] RepositoryNames { get; set; }
        public string RepositoryId { get; set; }
        public string Tag { get; set; }
        public string EmptyValueType { get; set; }
    }
}
