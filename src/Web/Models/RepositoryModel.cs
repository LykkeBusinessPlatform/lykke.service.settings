using Core.Extensions;
using Core.Repository;

namespace Web.Models
{
    public class RepositoryModel
    {
        public PaginatedList<IRepository> Repositories { get; set; }
        public string ServiceUrlForViewMode { get; set; }
        public string RepositoryFileInfoControllerAction { get; set; }
    }
}
