using Core.Entities;

namespace Web.Models
{
    public class RoleModel : IRoleEntity
    {
        public string RoleId { get; set; }
        public string Name { get; set; }
        public IRoleKeyValue[] KeyValues { get; set; }
    }
}
