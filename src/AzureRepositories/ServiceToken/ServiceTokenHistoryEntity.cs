using Core.Entities;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.ServiceToken
{
    public class ServiceTokenHistoryEntity : TableEntity, IServiceTokenHistory
    {
        public string TokenId { get; set; }

        public string UserName { get; set; }
        public string KeyOne { get; set; }
        public string KeyTwo { get; set; }
        public string UserIpAddress { get; set; }
    }
}
