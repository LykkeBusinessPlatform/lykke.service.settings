using System.Collections.Generic;
using Core.Entities;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace AzureRepositories.User
{
    public class RoleEntity : TableEntity, IRoleEntity
    {
        private string _roleId;

        public static string GeneratePartitionKey() => "UR";
        public static string GenerateRowKey(string roleId) => roleId;

        public string RoleId
        {
            get => _roleId ?? RowKey;
            set => _roleId = value;
        }
        public string Name { get; set; }
        public IRoleKeyValue[] KeyValues { get; set; }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            if (properties.TryGetValue(nameof(RoleId), out var roleId))
                RoleId = roleId.StringValue;

            if (properties.TryGetValue(nameof(KeyValues), out var keyValues))
            {
                var json = keyValues.StringValue;
                if (!string.IsNullOrEmpty(json))
                {
                    KeyValues = JsonConvert.DeserializeObject<RoleKeyValue[]>(json);
                }
            }

            if (properties.TryGetValue(nameof(Name), out var name))
                Name = name.StringValue;
        }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var dict = new Dictionary<string, EntityProperty>
            {
                {nameof(RoleId), new EntityProperty(RoleId)},
                {nameof(KeyValues), new EntityProperty(JsonConvert.SerializeObject(KeyValues))},
                {nameof(Name), new EntityProperty(Name)}
            };

            return dict;
        }
    }

    public class RoleKeyValue : IRoleKeyValue
    {
        public string RowKey { get; set; }
        public bool HasFullAccess { get; set; }
    }
}
