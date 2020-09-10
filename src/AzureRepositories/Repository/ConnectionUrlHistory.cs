using Core.Entities;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Repository
{
    public class ConnectionUrlHistory : TableEntity, IConnectionUrlHistory
    {
        private string _id;
        private string _datetime;

        public static string GeneratePartitionKey() => "CUH";

        public static string GenerateRowKey(string connectionUrlHistoryId) => connectionUrlHistoryId;

        public string Id
        {
            get => _id ?? RowKey;
            set => _id = value;
        }
        public string Ip { get; set; }
        public string RepositoryId { get; set; }
        public string UserAgent { get; set; }
        public string Datetime
        {
            get => _datetime ?? Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            set => _datetime = value;
        }
    }
}
