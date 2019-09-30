using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Token
{
    public interface ITokensRepository
    {
        Task<IToken> GetAsync(string tokenId);
        Task<IEnumerable<IToken>> GetAllAsync();
        Task RemoveTokenAsync(string tokenId);
        Task SaveTokenAsync(IToken token);
    }
}
