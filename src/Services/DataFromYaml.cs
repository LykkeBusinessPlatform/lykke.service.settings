using System.Collections.Generic;
using Core.Models;

namespace Services
{
    public class DataFromYaml
    {
        public string Json { get; set; }
        public List<KeyValue> Placeholders { get; set; }
    }
}
