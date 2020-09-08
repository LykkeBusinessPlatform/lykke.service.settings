using System;
using System.Collections.Generic;
using System.Text;
using Core.Entities;
using Core.KeyValue;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace AzureRepositories.User
{
    public class RoleEntity : TableEntity, IRoleEntity
    {
        public static string GeneratePartitionKey() => "UR";
        public static string GenerateRowKey (string roleId) => roleId;

        public string Name { get; set; }
        public IRoleKeyValue[] KeyValues { get; set; }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            if (properties.TryGetValue("KeyValues", out var keyValues))
            {
                var json = keyValues.StringValue;
                if (!string.IsNullOrEmpty(json))
                {
                    KeyValues = JsonConvert.DeserializeObject<RoleKeyValue[]>(json);
                }
            }

            if (properties.TryGetValue("Name", out var name))
            {
                Name = name.StringValue;
            }
        }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var dict = new Dictionary<string, EntityProperty>
            {
                {"KeyValues", new EntityProperty(JsonConvert.SerializeObject(KeyValues))},
                {"Name", new EntityProperty(Name)}
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
