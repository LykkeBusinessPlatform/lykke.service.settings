namespace Core.Entities
{
    public interface IRoleKeyValue
    {
        string RowKey { get; set; }
        bool HasFullAccess { get; set; }
    }
}
