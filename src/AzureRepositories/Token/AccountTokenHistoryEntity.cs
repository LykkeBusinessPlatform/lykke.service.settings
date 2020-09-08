using Core.Entities;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Token
{
    public class AccountTokenHistoryEntity : TableEntity, IAccountTokenHistory
    {
        public string TokenId
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }

        public string UserName { get; set; }
        public string AccessList { get; set; }
        public string IpList { get; set; }
        public string UserIpAddress { get; set; }
    }
}
