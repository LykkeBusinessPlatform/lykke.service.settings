using Common.Log;
using Core.KeyValue;
using JetBrains.Annotations;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using System.Threading.Tasks;
using Shared.Commands;
using System.Diagnostics;
using System;
using Lykke.Common.Log;

namespace Shared.RabbitPublishers
{
    [UsedImplicitly]
    public class SettingsPublisher : ISettingsPublisher
    {
        private readonly ILog _log;
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private readonly string _serviceName;
        private readonly bool _canPublish;

        private RabbitMqPublisher<string> _publisher;

        public SettingsPublisher(
            ILogFactory logFactory,
            string connectionString,
            string exchangeName,
            string serviceName,
            bool canPublish)
        {
            _canPublish = canPublish;
            if (!_canPublish)
                return;

            _log = logFactory.CreateLog(this);
            _connectionString = connectionString;
            _exchangeName = exchangeName;
            _serviceName = serviceName;
        }

        public void Start()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (_canPublish)
            {
                var settings = RabbitMqSubscriptionSettings
                .CreateForPublisher(_connectionString, _exchangeName)
                .MakeDurable();

                _publisher = new RabbitMqPublisher<string>(settings)
                    .SetSerializer(new JsonMessageSerializer<string>())
                    .SetPublishStrategy(new DefaultFanoutPublishStrategy(settings))
                    .PublishSynchronously()
                    .SetLogger(_log)
                    .Start();
            }

            stopwatch.Stop();
            Console.WriteLine($"----- SettingsPublisher init finished: {stopwatch.ElapsedMilliseconds} ms -----");
        }

        public void Dispose()
        {
            _publisher?.Dispose();
        }

        public void Stop()
        {
            _publisher?.Stop();
        }

        public async Task PublishAsync(IKeyValueEntity entity, string command)
        {
            var commandStr = $"{Commands.Commands.KeyValueCommand} \"{entity.RowKey}\" {command} \"{entity.Value}\" {Commands.Commands.FromCommand} \"{_serviceName}\"";

            await _publisher.ProduceAsync(commandStr);
        }

        public async Task PublishAsync(string message)
        {
            await _publisher.ProduceAsync(message);
        }
    }
}
