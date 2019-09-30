using System;
using System.Collections.Generic;
using System.Text;

namespace Web.Models
{
    public class ConnectionUrlHistoryModel
    {
        public string RowKey { get; set; }
        public string Ip { get; set; }
        public string RepositoryId { get; set; }
        public string UserAgent { get; set; }
        public string Timestamp { get; set; }
    }
}
