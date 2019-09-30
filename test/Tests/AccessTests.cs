using Services.JsonShaper;
using Xunit;

namespace Tests
{
    public class AccessTests
    {
        [Fact]
        public void AccessWithWildCard() 
        {
            var hasAsses = "127.0.0.1".HasAccessByIp("45.56.34.45","*");
            Assert.True(hasAsses);
        }

        [Fact]
        public void AccessByDirectIp() 
        {
            var hasAsses = "127.0.0.1".HasAccessByIp("44.55.66.77","127.0.0.1");
            Assert.True(hasAsses);
        }

        [Fact]
        public void AccessByIpWithWildCard() 
        {
            var hasAsses = "127.0.0.1".HasAccessByIp("44.55.66.77","127.0.*");
            Assert.True(hasAsses);
        }
    }
}
