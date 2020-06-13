using Larva.MessageProcess.Messaging;
using Larva.MessageProcess.RabbitMQ.Eventing;
using RabbitMQTopic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Larva.MessageProcess.RabbitMQ.Infrastructure
{
    public class EventBus : IEventBus
    {
        private Producer _producer;
        private string _topic;

        public void Initialize(ProducerSettings producerSettings, string topic, int queueCount = 4)
        {
            _topic = topic;
            _producer = new Producer(producerSettings);
            _producer.RegisterTopic(topic, queueCount);
        }

        public void Start()
        {
            _producer.Start();
        }

        public void Shutdown()
        {
            _producer.Shutdown();
        }

        public async Task PublishAsync(EventStream eventStream)
        {
            var eventStreamMessage = new EventStreamMessage
            {
                Id = eventStream.Id,
                BusinessKey = eventStream.BusinessKey,
                Timestamp = eventStream.Timestamp,
                ExtraDatas = eventStream.ExtraDatas,
                Events = eventStream.Events.ToDictionary(kv => kv.Id, kv => new EventMessage
                {
                    EventTypeName = kv.GetMessageTypeName(),
                    EventData = Newtonsoft.Json.JsonConvert.SerializeObject(kv)
                }),
            };
            var body = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(eventStreamMessage));
            var message = new Message(_topic, 2, body, "text/json");
            await _producer.SendMessageAsync(message, eventStream.BusinessKey);
        }
    }
}