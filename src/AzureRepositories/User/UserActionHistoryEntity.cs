using System;
using AzureRepositories.Extensions;
using Core.Entities;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.User
{
    public class UserActionHistoryEntity : TableEntity, IUserActionHistoryEntity
    {
        public static string GeneratePartitionKey() => "UAH";
        public string GetRawKey() => ActionDate.StorageString();

        public string UserEmail { get; set; }
        public DateTime ActionDate { get; set; }
        public string IpAddress { get; set; }
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public string Params { get; set; }

        public static UserActionHistoryEntity Create(IUserActionHistoryEntity entity)
        {
            return new UserActionHistoryEntity
            {
                UserEmail = entity.UserEmail,
                ActionDate = entity.ActionDate,
                IpAddress = entity.IpAddress,
                ControllerName = entity.ControllerName,
                ActionName = entity.ActionName,
                Params = entity.Params,
                PartitionKey = GeneratePartitionKey(),
                RowKey = entity.ActionDate.StorageString()
            };
        }
    }
}
