using Core.Entities;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.ServiceToken
{
    public class ServiceTokenEntity : TableEntity, IServiceTokenEntity
    {
        private string _token;

        public static string GeneratePartitionKey() => "S";

        public string Token
        {
            get => _token ?? RowKey;
            set => _token = value;
        }
        public string SecurityKeyOne { get; set; }
        public string SecurityKeyTwo { get; set; }
    }
}
