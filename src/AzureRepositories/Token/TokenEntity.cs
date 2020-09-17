using Core.Entities;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Token
{
    public class TokenEntity : TableEntity, IToken
    {
        private string _tokenId;

        public static string GeneratePartitionKey() => "A";

        public static string GenerateRowKey(string tokenId) => tokenId;

        public string AccessList { get; set; }
        public string IpList { get; set; }

        public string TokenId
        {
            get => _tokenId ?? RowKey;
            set => _tokenId = value;
        }
    }
}
