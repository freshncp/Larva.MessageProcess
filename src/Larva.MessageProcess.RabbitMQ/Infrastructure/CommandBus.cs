using Larva.MessageProcess.Messaging;
using Larva.MessageProcess.RabbitMQ.Commanding;
using RabbitMQTopic;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Larva.MessageProcess.RabbitMQ.Infrastructure
{
    public class CommandBus : ICommandBus
    {
        private Producer _producer;
        private string _topic;
        private IPEndPoint _replyAddress;

        public void Initialize(ProducerSettings producerSettings, string topic, int queueCount, IPEndPoint replyAddress)
        {
            _topic = topic;
            _producer = new Producer(producerSettings);
            _producer.RegisterTopic(topic, queueCount);
            _replyAddress = replyAddress;
        }

        public void Start()
        {
            _producer.Start();
        }

        public void Shutdown()
        {
            _producer.Shutdown();
        }

        public async Task SendAsync(ICommand command)
        {
            var commandData = new CommandMessage
            {
                CommandTypeName = command.GetMessageTypeName(),
                CommandData = Newtonsoft.Json.JsonConvert.SerializeObject(command),
                ExtraDatas = new Dictionary<string, string>
                {
                    { "ReplyAddress", _replyAddress.ToString() }
                }
            };
            var body = System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(commandData));
            var message = new Message(_topic, 1, body, "text/json");
            await _producer.SendMessageAsync(message, command.BusinessKey);
        }
    }
}