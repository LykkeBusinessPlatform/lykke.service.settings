using Autofac;
using Common;
using System.Threading.Tasks;

namespace Shared.RabbitSubscribers
{
    public interface ISettingsSubscriber: IStartable, IStopable
    {
        Task ProcessMessageAsync(string arg);
    }
}
