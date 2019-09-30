namespace Core.Token
{
    public interface IToken : IEntity
    {
        string IpList {get;set;}
        string AccessList {get;set;}
    }
}
