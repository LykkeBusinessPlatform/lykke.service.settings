namespace Core.User
{
    public interface IRoleEntity:IEntity
    {
        string Name { get; set; }
        IRoleKeyValue[] KeyValues { get; set; }
    }

    public interface IRoleKeyValue
    {
        string RowKey { get; set; }
        bool HasFullAccess { get; set; }
    }
}
