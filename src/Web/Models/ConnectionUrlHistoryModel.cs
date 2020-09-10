using Core.Entities;

namespace Web.Models
{
    public class ConnectionUrlHistoryModel : IConnectionUrlHistory
    {
        public string Id { get; set; }
        public string Ip { get; set; }
        public string RepositoryId { get; set; }
        public string UserAgent { get; set; }
        public string Datetime { get; set; }
    }
}
