namespace Web.Models
{
    public class UserModel
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool Active { get; set; }
        public bool Admin { get; set; }
        public string[] Roles { get; set; }
    }
}
