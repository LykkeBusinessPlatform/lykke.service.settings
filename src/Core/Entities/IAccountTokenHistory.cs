namespace Core.Entities
{
    public interface IAccountTokenHistory
    {
        string TokenId { get; set; }
        string AccessList { get; set; }
        string IpList { get; set; }
        string UserName { get; set; }
        string UserIpAddress { get; set; }
    }
}
