using System.Collections.Generic;
using Core.Entities;
using Core.Enums;

namespace Web.Models
{
    public class ApiRepositoryModel
    {
        public UpdateSettingsStatus Status { get; set; }
        public string Message { get; set; }
        public IEnumerable<IRepository> Repositories { get; set; }
    }
}
