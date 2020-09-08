using Core.Entities;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Token
{
    public class TokenEntity : TableEntity, IToken
    {
        public static string GeneratePartitionKey() => "A";

        public static string GenerateRowKey(string tokenId) => tokenId;

        public string AccessList {get;set;}
        public string IpList {get;set;}
    }
}
