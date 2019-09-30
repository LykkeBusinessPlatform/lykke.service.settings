using System;
using Core.Lock;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Lock
{
    public class LockEntity : TableEntity, ILockEntity
    {
        public static string GeneratePartitionKey() => "L";

        public DateTime DateTime { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string IpAddress { get; set; }
    }
}
