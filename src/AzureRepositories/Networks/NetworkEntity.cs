using System;
using Core.Networks;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Networks
{
    public class NetworkEntity : TableEntity, INetwork
    {
        public string Id => RowKey;
        public string Name { get; set; }
        public string Ip { get; set; }

        internal static string GeneratePartitionKey() => "Network";
        internal static string GenerateRowKey(string id) => id;

        internal static NetworkEntity Create(INetwork src) => new NetworkEntity
        {
            PartitionKey = GeneratePartitionKey(),
            RowKey = src.Id ?? Guid.NewGuid().ToString(),
            Name = src.Name,
            Ip = src.Ip
        };
        
        internal static Network ToDomain(NetworkEntity src) => new Network
        {
            Id = src.Id,
            Name = src.Name,
            Ip = src.Ip
        };
    }
}
