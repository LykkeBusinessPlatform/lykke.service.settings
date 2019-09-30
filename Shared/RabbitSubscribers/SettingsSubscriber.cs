using Autofac;
using AzureRepositories.KeyValue;
using Common;
using Common.Log;
using Core.Command;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Shared.Commands;
using Shared.RabbitPublishers;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Shared.RabbitSubscribers
{
    [UsedImplicitly]
    public class SettingsSubscriber: ISettingsSubscriber
    {
        private readonly ISettingsPublisher _publisher;
        private readonly ILog _log;
        private readonly string _connectionString;
        private readonly ICommand _command;
        private readonly string _exchangeName;
        private readonly string _serviceName;
        private RabbitMqSubscriber<string> _subscriber;

        public SettingsSubscriber(
            ISettingsPublisher publisher,
            ILogFactory logFactory,
            string connectionString,
            string exchangeName,
            string serviceName,
            ICommand command)
        {
            _publisher = publisher;
            _log = logFactory.CreateLog(this);
            _connectionString = connectionString;
            _exchangeName = exchangeName;
            _command = command;
            _serviceName = serviceName;
        }

        public void Start()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var settings = RabbitMqSubscriptionSettings
                .CreateForSubscriber(_connectionString, _exchangeName, _serviceName.ToLower())
                .MakeDurable();

            _subscriber = new RabbitMqSubscriber<string>(
                settings,
                new ResilientErrorHandlingStrategy(_log, settings,
                    retryTimeout:TimeSpan.FromSeconds(10),
                    next: new DeadQueueErrorHandlingStrategy(_log, settings)))
                .SetMessageDeserializer(new JsonMessageDeserializer<string>())
                .SetMessageReadStrategy(new MessageReadQueueStrategy())
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding()
                .SetLogger(_log)
                .Start();

            stopwatch.Stop();
            Console.WriteLine($"----- SetitngsSubscriber init finished: {stopwatch.ElapsedMilliseconds} ms -----");
        }

        public async Task ProcessMessageAsync(string arg)
        {
            try
            {
                var commands = ParseArguments(arg);

                var keyValueEntity = new KeyValueEntity();
                var generate = false;
                string fromService = "";
                string command = "";

                for (int i = 0; i < commands.Length; i++)
                {
                    if (i < commands.Length - 1)
                    {
                        if (commands[i] == Commands.Commands.KeyValueCommand)
                        {
                            keyValueEntity.RowKey = commands[i + 1];
                        }
                        else if (commands[i] == Commands.Commands.SetValueCommand)
                        {
                            keyValueEntity.Value = commands[i + 1];
                        }
                        else if (commands[i] == Commands.Commands.FromCommand)
                        {
                            fromService = commands[i + 1];
                        }
                    }

                    if (Commands.Commands.AllowedMetadatasToGenerate.ContainsValue(commands[i]))
                    {
                        generate = true;
                        command = commands[i];
                    }
                }

                // check if message came from anoter instance and apply commands
                if (_serviceName != fromService)
                {
                    _log.Info("Command received", context: arg);

                    if (generate)
                    {
                        await _command.GenerateValue(keyValueEntity, command, fromService);
                    }
                    else if (keyValueEntity.RowKey != String.Empty && keyValueEntity.Value != String.Empty)
                    {
                        await _command.SetValue(keyValueEntity, fromService);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: arg);
                throw;
            }
        }

        public void Dispose()
        {
            _subscriber?.Dispose();
        }

        public void Stop()
        {
            _subscriber.Stop();
        }

        private static string[] ParseArguments(string commandLine)
        {
            char[] parmChars = commandLine.ToCharArray();
            bool inQuote = false;
            for (int index = 0; index < parmChars.Length; index++)
            {
                if (parmChars[index] == '"')
                    inQuote = !inQuote;
                if (!inQuote && parmChars[index] == ' ')
                    parmChars[index] = '\n';
            }
            return (new string(parmChars)).Replace("\"", "").Split('\n');
        }
    }
}
