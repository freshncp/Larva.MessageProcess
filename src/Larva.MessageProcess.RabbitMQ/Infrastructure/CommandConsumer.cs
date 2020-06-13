using log4net;
using Larva.MessageProcess.Interception;
using Larva.MessageProcess.Messaging;
using Larva.MessageProcess.Processing;
using Larva.MessageProcess.RabbitMQ.Commanding;
using Newtonsoft.Json;
using RabbitMQTopic;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Larva.MessageProcess.RabbitMQ.Infrastructure
{
    public class CommandConsumer
    {
        private DefaultMessageProcessor _commandProcessor;
        private CommandHandlerProvider _commandHandlerProvider;
        private Consumer _consumer;
        private ILog _logger = LogManager.GetLogger(typeof(CommandConsumer));

        public void Initialize(ConsumerSettings consumerSettings, string topic, int queueCount, IInterceptor[] interceptors, params Assembly[] assemblies)
        {
            _consumer = new Consumer(consumerSettings);
            _consumer.Subscribe(topic, queueCount);
            _commandHandlerProvider = new CommandHandlerProvider();
            _commandHandlerProvider.Initialize(interceptors, assemblies);
            var processingMessageHandler = new DefaultProcessingMessageHandler();
            processingMessageHandler.Initialize(_commandHandlerProvider);
            _commandProcessor = new DefaultMessageProcessor();
            _commandProcessor.Initialize(processingMessageHandler);
        }

        public void Start()
        {
            _consumer.OnMessageReceived += (sender, e) =>
            {
                var body = System.Text.Encoding.UTF8.GetString(e.Context.GetBody());
                try
                {
                    var commandMessage = JsonConvert.DeserializeObject<CommandMessage>(body);
                    var messageTypes = _commandHandlerProvider.GetMessageTypes();
                    if (!messageTypes.ContainsKey(commandMessage.CommandTypeName))
                    {
                        _logger.Warn($"Command type not found: {commandMessage.CommandTypeName}, Body={body}");
                        e.Context.Ack();
                        return;
                    }
                    var messageType = messageTypes[commandMessage.CommandTypeName];
                    var command = (ICommand)JsonConvert.DeserializeObject(commandMessage.CommandData, messageType);
                    command.MergeExtraDatas(commandMessage.ExtraDatas);
                    var processingCommand = new ProcessingMessage(command, string.Empty, new CommandExecutingContext(_logger, e.Context), commandMessage.ExtraDatas, true);
                    _commandProcessor.Process(processingCommand);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Consume command fail: {ex.Message}, Body={body}", ex);
                }
            };
            _commandProcessor.Start();
            _consumer.Start();
        }

        public void Shutdown()
        {
            _consumer.Shutdown();
            _commandProcessor.Stop();
        }

        internal class CommandExecutingContext : IMessageExecutingContext
        {
            private string _result;
            private ILog _logger;
            private IMessageTransportationContext _transportationContext;

            public CommandExecutingContext(ILog logger, IMessageTransportationContext transportationContext)
            {
                _logger = logger;
                _transportationContext = transportationContext;
            }

            public string GetResult()
            {
                return _result;
            }

            public void SetResult(string result)
            {
                _result = result;
            }

            public Task NotifyMessageExecutedAsync(MessageExecutingResult messageResult)
            {
                if (messageResult.Status == MessageExecutingStatus.Success)
                {
                    _transportationContext.Ack();
                    _logger.Debug($"Result={messageResult}\r\nRawMessage={JsonConvert.SerializeObject(messageResult.RawMessage)}");
                }
                else
                {
                    _logger.Error($"Result={messageResult}\r\nRawMessage={JsonConvert.SerializeObject(messageResult.RawMessage)}\r\n{messageResult.StackTrace}");
                }
                return Task.CompletedTask;
            }
        }
    }
}
