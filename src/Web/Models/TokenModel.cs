using Core.Entities;

namespace Web.Models
{
    public class TokenModel : IToken
    {
        public string TokenId { get; set; }

        public string IpList { get; set; }
        public string AccessList { get; set; }
    }
}
