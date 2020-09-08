namespace Core.Entities
{
    public interface IUserEntity
    {
        string Email { get; set; }
        string Salt { get; set; }
        string PasswordHash { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        bool? Active { get; set; }
        bool? Admin { get; set; }
        string[] Roles { get; set; }
    }
}
