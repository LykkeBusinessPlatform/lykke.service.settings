using System.Collections.Generic;
using Core.Token;

namespace Web.Models
{
    public class AccessTokenModel
    {
        public List<IToken> Tokens { get; set; }
        public string ServiceUrlForViewMode { get; set; }
    }
}
