using System;
using Core.Entities;

namespace Core.Models
{
    public class RepositoryUpdateHistory : IRepositoryUpdateHistory
    {
        public string RepositoryId { get; set; }

        public string InitialCommit { get; set; }

        public string User { get; set; }

        public string Branch { get; set; }

        public bool IsManual { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
