namespace Core.Entities
{
    public interface IRoleEntity
    {
        string RoleId { get; set; }
        string Name { get; set; }
        IRoleKeyValue[] KeyValues { get; set; }
    }
}
