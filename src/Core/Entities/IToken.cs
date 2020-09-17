namespace Core.Entities
{
    public interface IToken
    {
        string TokenId { get; }
        string IpList { get; set; }
        string AccessList { get; set; }
    }
}
