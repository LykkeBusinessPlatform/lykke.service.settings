using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Core.Networks
{
    public class Network : INetwork
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Ip { get; set; }

        public string[] Ips => Ip.Split(new[] {';', ','}, StringSplitOptions.RemoveEmptyEntries).Select(item => item.Trim()).ToArray();

        public bool IsValidIps()
        {
            return Ips.All(ip => IPAddress.TryParse(ip, out var _) || Regex.IsMatch(ip, @"^[0-9]{1,3}\.[0-9]{0,3}\.?[0-9]{0,3}\.?[0-9]{0,3}$"));
        }
    }
}
