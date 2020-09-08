using System;
using AzureRepositories.Extensions;
using Core.Entities;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.User
{
    public class UserSignInHistoryEntity : TableEntity, IUserSignInHistoryEntity
    {
        public static string GeneratePartitionKey() => "UH";

        public string GetRawKey()
        {
            return SignInDate.StorageString();
        }

        public string UserEmail { get; set; }

        public DateTime SignInDate { get; set; }

        public string IpAddress { get; set; }
    }
}
