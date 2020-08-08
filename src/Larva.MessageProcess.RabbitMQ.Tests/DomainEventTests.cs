using Larva.MessageProcess.RabbitMQ.Eventing;
using Larva.MessageProcess.RabbitMQ.Infrastructure;
using Larva.MessageProcess.RabbitMQ.Tests.DomainEvents;
using RabbitMQTopic;
using System;
using System.Threading;
using Xunit;

namespace Larva.MessageProcess.RabbitMQ.Tests
{
    public class DomainEventTests
    {
        [Fact]
        public void Test1()
        {
            //Larva.MessageProcess.LoggerManager.SetLoggerProvider(new Log4NetLoggerProvider());
            var consumer1 = new EventConsumer();
            consumer1.Initialize(new ConsumerSettings
            {
                AmqpUri = new Uri("amqp://demo:123456@localhost/test")
            }, "MessageProcess_EventTopic", 4, 5, null, typeof(DomainEventTests).Assembly);
            consumer1.Start();

            var consumer2 = new EventConsumer();
            consumer2.Initialize(new ConsumerSettings
            {
                AmqpUri = new Uri("amqp://demo:123456@localhost/test"),
                GroupName = "Subscriber2"
            }, "MessageProcess_EventTopic", 4, 5, null, typeof(DomainEventTests).Assembly);
            consumer2.Start();

            var eventBus = new EventBus();
            eventBus.Initialize(new ProducerSettings
            {
                AmqpUri = new Uri("amqp://demo:123456@localhost/test")
            }, "MessageProcess_EventTopic");
            eventBus.Start();
            for (var i = 1; i <= 10; i++)
            {
                for (var j = 1; j <= 10; j++)
                {
                    eventBus.PublishAsync(new DomainEvent1($"Test{i}")).Wait();
                }
            }
            Thread.Sleep(1000);
            eventBus.Shutdown();

            Thread.Sleep(2000);
            consumer1.Shutdown();
            consumer2.Shutdown();
        }
    }
}
