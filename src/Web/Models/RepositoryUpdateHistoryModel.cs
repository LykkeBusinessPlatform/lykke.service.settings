using System;
using System.Collections.Generic;
using System.Text;

namespace Web.Models
{
    class RepositoryUpdateHistoryModel
    {
        public string RowKey { get; set; }
        public string User { get; set; }
        public string Commit { get; set; }
        public string Branch { get; set; }
        public bool IsManual { get; set; }
        public string Timestamp { get; set; }
    }
}
