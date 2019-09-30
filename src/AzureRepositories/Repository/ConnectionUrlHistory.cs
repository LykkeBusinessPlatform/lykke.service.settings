using Core.Repository;
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace AzureRepositories.Repository
{
    public class ConnectionUrlHistory : TableEntity, IConnectionUrlHistory
    {
        public static string GeneratePartitionKey() => "CUH";

        public static string GenerateRowKey(string connectionUrlHistoryId) => connectionUrlHistoryId;

        public string Ip { get; set; }
        public string RepositoryId { get; set; }
        public string UserAgent { get; set; }
    }
}
