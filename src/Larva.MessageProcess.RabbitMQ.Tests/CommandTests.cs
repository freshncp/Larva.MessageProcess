using Larva.DynamicProxy.Interception;
using Larva.MessageProcess.RabbitMQ.Infrastructure;
using Larva.MessageProcess.RabbitMQ.Tests.Commands;
using RabbitMQTopic;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Larva.MessageProcess.RabbitMQ.Tests
{
    public class CommandTests
    {
        [Fact]
        public async Task Test1()
        {
            //Larva.MessageProcess.LoggerManager.SetLoggerProvider(new Log4NetLoggerProvider());
            var consumer = new CommandConsumer();
            consumer.Initialize(new ConsumerSettings
            {
                AmqpUri = new Uri("amqp://demo:123456@localhost/test")
            }, "MessageProcess_CommandTopic", 4, false, 1, new IInterceptor[] { new PerformanceCounterInterceptor() }, typeof(CommandTests).Assembly);
            consumer.Start();

            var commandBus = new CommandBus();
            commandBus.Initialize(new ProducerSettings
            {
                AmqpUri = new Uri("amqp://demo:123456@localhost/test")
            }, "MessageProcess_CommandTopic", 4, IPEndPoint.Parse("127.0.0.1:5000"));
            commandBus.Start();
            for (var i = 1; i <= 5; i++)
            {
                for (var j = 1; j <= 5; j++)
                {
                    await commandBus.SendAsync(new Command1($"Test{i}"));
                }
            }
            Thread.Sleep(1000);
            commandBus.Shutdown();

            Thread.Sleep(10000);
            consumer.Shutdown();
        }
    }
}
