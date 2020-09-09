using Core.Entities;

namespace Web.Models
{
    public class UserModel : IUserEntity
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool? Active { get; set; }
        public bool? Admin { get; set; }
        public string[] Roles { get; set; }
        public string Salt { get; set; }
        public string PasswordHash { get; set; }
    }
}
