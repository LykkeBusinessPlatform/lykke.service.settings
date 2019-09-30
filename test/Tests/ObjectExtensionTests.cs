using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories.KeyValue;
using AzureStorage.Tables;
using Common;
using Core.KeyValue;
using Newtonsoft.Json;
using Services;
using Web.Extensions;
using Xunit;

namespace Tests
{
    public class ObjectExtensionTests
    {
        [Fact]
        public void Get2KeysFromJson()
        {
            //Arrange
            var text = "{\"Ip\":\"${redisIpOnDev}:${redisPort}\"}";
            var jsonObj = JsonConvert.DeserializeObject(text);

            //Act
            var result = jsonObj.GetKeysDict();

            //Assert
            Assert.Equal("redisIpOnDev", result.First().Key);
            Assert.Equal(2, result.Count);
        }

    }
}
