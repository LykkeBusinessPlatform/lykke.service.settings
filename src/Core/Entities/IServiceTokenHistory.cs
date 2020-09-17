namespace Core.Entities
{
    public interface IServiceTokenHistory
    {
        string TokenId { get; set; }
        string KeyOne { get; set; }
        string KeyTwo { get; set; }
        string UserName { get; set; }
        string UserIpAddress { get; set; }
    }
}
