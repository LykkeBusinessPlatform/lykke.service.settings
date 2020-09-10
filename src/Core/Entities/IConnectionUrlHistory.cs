namespace Core.Entities
{
    public interface IConnectionUrlHistory
    {
        string Id { get; set; }
        string Ip { get; set; }
        string RepositoryId { get; set; }
        string UserAgent { get; set; }
        string Datetime { get; set; }
    }
}
