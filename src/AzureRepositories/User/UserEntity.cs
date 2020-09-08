using System.Collections.Generic;
using Core.Entities;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace AzureRepositories.User
{
    public class UserEntity : TableEntity, IUserEntity
    {
        public static string GeneratePartitionKey() => "U";

        public static string GenerateRowKey(string userEmail) => userEmail.ToLowerInvariant();

        public string Email { get; set; }
        public string Salt { get; set; }
        public string PasswordHash { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool? Active { get; set; }
        public bool? Admin { get; set; }
        public string[] Roles { get; set; }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            if (properties.TryGetValue(nameof(Email), out var email))
                Email = email.StringValue;

            if (properties.TryGetValue(nameof(Salt), out var salt))
                Salt = salt.StringValue;

            if (properties.TryGetValue(nameof(PasswordHash), out var passwordHash))
                PasswordHash = passwordHash.StringValue;

            if (properties.TryGetValue(nameof(FirstName), out var firstName))
                FirstName = firstName.StringValue;

            if (properties.TryGetValue(nameof(LastName), out var lastName))
                LastName = lastName.StringValue;

            if (properties.TryGetValue(nameof(Active), out var active))
                Active = active.BooleanValue;

            if (properties.TryGetValue(nameof(Admin), out var admin))
                Admin = admin.BooleanValue;

            if (properties.TryGetValue(nameof(Roles), out var roles))
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
                {"Email", new EntityProperty(Email)},
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
