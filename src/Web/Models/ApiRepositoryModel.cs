using Core.Repository;
using System.Collections.Generic;
using Core.Extensions;

namespace Web.Models
{
    public class ApiRepositoryModel
    {
        public UpdateSettingsStatus Status { get; set; }
        public string Message { get; set; }
        public IEnumerable<IRepository> Repositories { get; set; }
    }
}
