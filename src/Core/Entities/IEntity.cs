namespace Core.Entities
{
    public interface IEntity
    {
        string RowKey { get; set; }

        string ETag { get; set; }
    }
}
