using Core.Entities;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.ServiceToken
{
    public class ServiceTokenEntity : TableEntity, IServiceTokenEntity
    {
        public static string GeneratePartitionKey() => "S";

        public string Token { get; set; }
        public string SecurityKeyOne { get; set; }
        public string SecurityKeyTwo { get; set; }
    }
}
