namespace Core.Entities
{
    public interface IRoleEntity : IEntity
    {
        string Name { get; set; }
        IRoleKeyValue[] KeyValues { get; set; }
    }
}
