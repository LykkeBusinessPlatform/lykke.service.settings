using System.Collections.Generic;
using System.Threading.Tasks;
using AzureRepositories.KeyValue;
using AzureRepositories.Networks;
using AzureStorage.Tables;
using Common;
using Core.Entities;
using Core.Models;
using Core.Repositories;
using Newtonsoft.Json;
using Services;
using Xunit;

namespace Tests
{
    public class SubstitureAndNetworksTests
    {
        private INetworkRepository _networkRepository;

        public SubstitureAndNetworksTests()
        {
            InitNetworkRepository();
        }

        [Fact]
        public async Task Network_IsGetByIp()
        {
            string ip = "::1";
            var network = await _networkRepository.GetByIpAsync(ip);

            Assert.NotNull(network);
            Assert.Equal("1", network.Id);
            Assert.Equal("Local", network.Name);
        }

        [Fact]
        public async Task Network_IsGetByPartIp()
        {
            string ip = "192.168.0.25";
            var network = await _networkRepository.GetByIpAsync(ip);

            Assert.NotNull(network);
            Assert.Equal("1", network.Id);
            Assert.Equal("Local", network.Name);
        }

        [Fact]
        public void NoSubstitute_IsOK()
        {
            string json = "{'Service': {'ServiceUrl': '${service1}'}, 'Val': '${val}'}";

            var keyValues = new Dictionary<string, IKeyValueEntity>
            {
                {"service1", new KeyValueEntity
                {
                    Value = "127.0.0.1",
                    Override = new[] {new OverrideValue{NetworkId = "1", Value = "192.168.1.1"}}
                }},
                {"val", new KeyValueEntity {Value = "5"}}
            };

            string result = json.Substitute(keyValues, "5");

            Assert.Equal("{'Service': {'ServiceUrl': '127.0.0.1'}, 'Val': '5'}", result);
        }

        [Fact]
        public void CommonSubstitute_IsOK()
        {
            string json = "{'Service': {'ServiceUrl': '${service1}'}, 'Val': '${val}'}";
            var keyValues = new Dictionary<string, IKeyValueEntity>
            {
                {"service1", new KeyValueEntity
                {
                    Value = "127.0.0.1",
                    Override = new[] {new OverrideValue{NetworkId = "1", Value = "192.168.1.1"}}
                }},
                {"val", new KeyValueEntity {Value = "5"}}
            };

            string result = json.Substitute(keyValues);

            Assert.Equal("{'Service': {'ServiceUrl': '127.0.0.1'}, 'Val': '5'}", result);
        }

        [Fact]
        public void CommonSubstitute_IsOKQuotesReplaced()
        {
            string json = "{\"Str1\": \"val ${string}:6379\","+
                            "\"Str2\": \"val ${string}\","+
                            "\"Str3\": \"${string}\","+
                            "\"Str4\": \"${string1}${string2}\","+
                            "\"Str5\": \"${string1}${string}${string2}\","+
                            "\"TestInt\": \"${int}\"," +
                            "\"TestDouble\": \"${double}\"," +
                            "\"TestBool\": \"${bool1}\"," +
                            "\"TestBool1\": \"${bool2}\"}}";
            var keyValues = new Dictionary<string, IKeyValueEntity>
            {
                {"string", new KeyValueEntity {Value = "127.0.0.1"}},
                {"int", new KeyValueEntity {Value = "5"}},
                {"double", new KeyValueEntity {Value = "10.2"}},
                {"bool1", new KeyValueEntity {Value = "True"}},
                {"bool2", new KeyValueEntity {Value = "false"}}
            };

            string result = json.Substitute(keyValues);
            string expected = "{\"Str1\": \"val 127.0.0.1:6379\"," +
                              "\"Str2\": \"val 127.0.0.1\"," +
                              "\"Str3\": \"127.0.0.1\"," +
                              "\"Str4\": \"${string1}${string2}\"," +
                              "\"Str5\": \"${string1}127.0.0.1${string2}\"," +
                              "\"TestInt\": 5," +
                              "\"TestDouble\": 10.2," +
                              "\"TestBool\": true," +
                              "\"TestBool1\": false}}";
            Assert.Equal(expected, result);
        }
        
        [Fact]
        public void CommonSubstituteNew_IsOK()
        {
            string json = "{'Service': {'ServiceUrl': '${service1}'}, 'Val': '${val}'}";
            var keyValues = new Dictionary<string, IKeyValueEntity>
            {
                {"service1", new KeyValueEntity
                {
                    Value = "127.0.0.1",
                    Override = new[] {new OverrideValue{NetworkId = "1", Value = "192.168.1.1"}}
                }},
                {"val", new KeyValueEntity {Value = "5"}}
            };

            string result = json.SubstituteNew(keyValues);

            Assert.Equal("{'Service': {'ServiceUrl': '127.0.0.1'}, 'Val': 5}", result);
        }

        [Fact]
        public void NetworkSubstitute_IsOK()
        {
            string json = "{'Service': {'ServiceUrl': '${service1}'}, 'Val': '${val}'}";

            var keyValues = new Dictionary<string, IKeyValueEntity>
            {
                {"service1", new KeyValueEntity
                {
                    Value = "127.0.0.1",
                    Override = new[] {new OverrideValue{NetworkId = "1", Value = "8.8.8.8"}}
                }},
                {"val", new KeyValueEntity {Value = "5"}}
            };

            string result = json.Substitute(keyValues, "1");

            Assert.Equal("{'Service': {'ServiceUrl': '8.8.8.8'}, 'Val': '5'}", result);
        }

        private void InitNetworkRepository()
        {
            _networkRepository = new NetworkRepository(new NoSqlTableInMemory<NetworkEntity>());
            _networkRepository.AddAsync(new Network { Id = "1", Ip = "127.0.0.1, 192.168.0, ::1", Name = "Local" }).Wait();
        }
    }
}
