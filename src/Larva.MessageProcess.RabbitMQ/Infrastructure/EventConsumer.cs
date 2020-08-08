using log4net;
using Larva.DynamicProxy.Interception;
using Larva.MessageProcess.Messaging;
using Larva.MessageProcess.Processing;
using Larva.MessageProcess.RabbitMQ.Eventing;
using Newtonsoft.Json;
using RabbitMQTopic;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Larva.MessageProcess.RabbitMQ.Infrastructure
{
    public class EventConsumer
    {
        private EventHandlerProvider _eventHandlerProvider;
        private DefaultMessageProcessor _eventStreamProcessor;
        private Consumer _consumer;
        private string _subscriber;
        private ILogger _logger = LoggerManager.GetLogger(typeof(EventConsumer));

        public void Initialize(ConsumerSettings consumerSettings, string topic, int queueCount, int retryIntervalSeconds, IInterceptor[] interceptors, params Assembly[] assemblies)
        {
            _consumer = new Consumer(consumerSettings);
            _consumer.Subscribe(topic, queueCount);
            _subscriber = consumerSettings.GroupName == null ? string.Empty : consumerSettings.GroupName;
            _eventHandlerProvider = new EventHandlerProvider();
            _eventHandlerProvider.Initialize(interceptors, assemblies);
            var processingMessageHandler = new DefaultProcessingMessageHandler();
            processingMessageHandler.Initialize(_eventHandlerProvider);
            _eventStreamProcessor = new DefaultMessageProcessor();
            _eventStreamProcessor.Initialize(processingMessageHandler, false, retryIntervalSeconds);
        }

        public void Start()
        {
            _consumer.OnMessageReceived += (sender, e) =>
            {
                var body = System.Text.Encoding.UTF8.GetString(e.Context.GetBody());
                try
                {
                    var eventStreamMessage = JsonConvert.DeserializeObject<EventStreamMessage>(body);
                    var messageTypes = _eventHandlerProvider.GetMessageTypes();
                    var messages = new List<IMessage>();
                    foreach (var eventMessage in eventStreamMessage.Events.Values)
                    {
                        if (!messageTypes.ContainsKey(eventMessage.EventTypeName))
                        {
                            _logger.Warn($"Event type not found: {eventMessage.EventTypeName}, Body={body}");
                        }
                        else
                        {
                            var messageType = messageTypes[eventMessage.EventTypeName];
                            var domainEvent = (IDomainEvent)JsonConvert.DeserializeObject(eventMessage.EventData, messageType);
                            domainEvent.MergeExtraDatas(eventStreamMessage.ExtraDatas);
                            messages.Add(domainEvent);
                        }
                    }
                    var messageGroup = new MessageGroup(eventStreamMessage.Id, eventStreamMessage.Timestamp, eventStreamMessage.BusinessKey, eventStreamMessage.ExtraDatas, messages, true);
                    var processingCommand = new ProcessingMessage(messageGroup, _subscriber, new EventExecutingContext(_logger, e.Context), eventStreamMessage.ExtraDatas);
                    _eventStreamProcessor.Process(processingCommand);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Consume event stream fail: {ex.Message}, Body={body}", ex);
                }
            };
            _eventStreamProcessor.Start();
            _consumer.Start();
        }

        public void Shutdown()
        {
            _consumer.Shutdown();
            _eventStreamProcessor.Stop();
        }

        internal class EventExecutingContext : IMessageExecutingContext
        {
            private string _result;
            private ILogger _logger;
            private IMessageTransportationContext _transportationContext;

            public EventExecutingContext(ILogger logger, IMessageTransportationContext transportationContext)
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
                    _logger.Info($"Result={messageResult}, RawMessage={JsonConvert.SerializeObject(messageResult.RawMessage)}");
                }
                else
                {
                    _logger.Error($"Result={messageResult}, RawMessage={JsonConvert.SerializeObject(messageResult.RawMessage)}\r\n{messageResult.StackTrace}");
                }
                return Task.CompletedTask;
            }
        }
    }
}