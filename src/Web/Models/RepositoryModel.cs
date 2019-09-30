using Core.Extensions;
using Core.Repository;
using System.Collections.Generic;
using Web.Extensions;

namespace Web.Models
{
    public class RepositoryModel
    {
        // public List<IRepository> Repositories { get; set; }
        public PaginatedList<IRepository> Repositories { get; set; }
        public string ServiceUrlForViewMode { get; set; }
        public string RepositoryFileInfoControllerAction { get; set; }
    }
}
