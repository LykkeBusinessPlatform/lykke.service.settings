using Core.Entities;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Repository
{
    public class RepositoryEntity : TableEntity, IRepository
    {
        private string _repositoryId;

        public static string GeneratePartitionKey() => "R";

        public static string GenerateRowKey(string repositoryId) => repositoryId;

        public string RepositoryId
        {
            get => _repositoryId ?? RowKey;
            set => _repositoryId = value;
        }
        public string LastModified { get; set; }
        public string Name { get; set; }
        public string GitUrl { get; set; }
        public string Branch { get; set; }
        public string FileName { get; set; }
        public string UserName { get; set; }
        public string ConnectionUrl { get; set; }
        public string OriginalName { get; set; }
        public bool UseManualSettings { get; set; }
        public string Tag { get; set; }
    }
}
