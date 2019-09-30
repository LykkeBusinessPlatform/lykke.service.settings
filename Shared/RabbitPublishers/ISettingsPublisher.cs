using System.Threading.Tasks;
using Autofac;
using Common;
using Core.KeyValue;

namespace Shared.RabbitPublishers
{
    public interface ISettingsPublisher : IStartable, IStopable
    {
        Task PublishAsync(string message);
        Task PublishAsync(IKeyValueEntity entity, string command);
    }
}
