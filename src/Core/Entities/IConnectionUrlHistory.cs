namespace Core.Entities
{
    public interface IConnectionUrlHistory : IEntity
    {
        string Ip { get; set; }
        string RepositoryId { get; set; }
        string UserAgent { get; set; }
    }
}
