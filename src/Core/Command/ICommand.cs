using Core.KeyValue;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Command
{
    public interface ICommand
    {
        Task GetKeyValue(IKeyValueEntity entity, string from = "");
        Task SetValue(IKeyValueEntity entity, string from = "");
        Task GenerateValue(IKeyValueEntity entity, string command, string from = "");
    }
}
