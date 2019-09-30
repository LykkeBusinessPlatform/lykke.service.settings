using System.Collections;
using System.Collections.Generic;
using Core.KeyValue;
using Core.User;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace AzureRepositories.User
{
    public class UserEntity : TableEntity, IUserEntity
    {
        public static string GeneratePartitionKey() => "U";

        public static string GenerateRowKey(string userEmail) => userEmail.ToLowerInvariant();

        public string Salt { get; set; }
        public string PasswordHash { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool? Active { get; set; }
        public bool? Admin { get; set; }
        public string[] Roles { get; set; }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            if (properties.TryGetValue("Salt", out var salt))
            {
                Salt = salt.StringValue;
            }

            if (properties.TryGetValue("PasswordHash", out var passwordHash))
            {
                PasswordHash = passwordHash.StringValue;
            }

            if (properties.TryGetValue("FirstName", out var firstName))
            {
                FirstName = firstName.StringValue;
            }

            if (properties.TryGetValue("LastName", out var lastName))
            {
                LastName = lastName.StringValue;
            }

            if (properties.TryGetValue("Active", out var active))
            {
                Active = active.BooleanValue;
            }

            if (properties.TryGetValue("Admin", out var admin))
            {
                Admin = admin.BooleanValue;
            }

            if (properties.TryGetValue("Roles", out var roles))
            {
                var json = roles.StringValue;
                if (!string.IsNullOrEmpty(json))
                {
                    Roles = JsonConvert.DeserializeObject<string[]>(json);
                }
            }
        }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var dict = new Dictionary<string, EntityProperty>
            {
                {"Salt", new EntityProperty(Salt)},
                {"PasswordHash", new EntityProperty(PasswordHash)},
                {"FirstName", new EntityProperty(FirstName)},
                {"LastName", new EntityProperty(LastName)},
                {"Active", new EntityProperty(Active)},
                {"Admin", new EntityProperty(Admin)},
                {"Roles", new EntityProperty(JsonConvert.SerializeObject(Roles))},
            };

            return dict;
        }
    }
}
