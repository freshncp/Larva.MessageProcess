using Larva.MessageProcess.Handling;
using Larva.MessageProcess.Messaging;
using Larva.MessageProcess.RabbitMQ.Commanding;
using Larva.MessageProcess.RabbitMQ.Tests.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Larva.MessageProcess.RabbitMQ.CommandHandlers
{
    public class CommandHandler1
        : ICommandHandler<Command1>
    {
        private volatile int _globalSequence;
        public async Task HandleAsync(Command1 message, IMessageContext ctx)
        {
            await Task.Delay(10);
            var globalSequence = Interlocked.Increment(ref _globalSequence);
            ctx.SetResult($"CommandHandler1 sequence={globalSequence}");
            Console.WriteLine($"{message.Id} CommandHandler1 handle Command1, replyAddress={message.GetExtraData("ReplyAddress")}");
            //throw new ApplicationException("Unknown error");
        }
    }
}