# Larva.MessageProcess

![.NET Core](https://github.com/freshncp/Larva.MessageProcess/workflows/.NET%20Core/badge.svg)

消息处理框架。不同业务键并行处理，相同业务键串行处理，思路来源于 [ENode](http://github.com/tangxuehua/enode)。

- 消息的消息类型名，默认为`typeof(TMessage).FullName`，可则通过标记`[MessageType("<name>")]` 自定义设置

- 支持多个订阅者，通过标记`[MessageSubscriber("<subscriber>")`到消息处理器来实现，订阅者之间并行消费

- 同一个消息、同一个消费者，如果有多个消息处理器，对于有执行顺序要求的，通过标记`[HandlePriority(1)]`来实现

- 消息是和消息处理器对应关系，如1:1、1:N，通过自定义实现 `MessageHandlerProviderBase` 来约束

- 支持消息执行结果返回

- 消息可以没有消息处理器，消息执行结果为 `HandlerNotFound`，使用者可以自行决定是否ACK

- 支持消息组概念，即将一组消息打包成一个消息 `MessageGroup`，进行发送和消费

- 相同 `BusinessKey` 的消息，如果处理失败，默认此类消息不会继续处理，必须前面的消息处理完才可以继续，可通过构造 `ProcessingMessage` 时设置 `continueWhenHandleFail`

- `IMessageHandler` 支持拦截器，需实现接口 `IInterceptor` 或直接继承 `StandardInterceptor`

## 开发日程

1）`MessageHandler` 的幂等支持

2）消息严格顺序支持

## 安装

```sh
dotnet add package MessageProcess
```

## 使用

```csharp
// 定义Command接口
public interface ICommand : IMessage { }

// 定义CommandHandler接口
public interface ICommandHandler<TCommand> : IMessageHandler<TCommand>
    where TCommand : class, ICommand
{ }

// CommandHandlerProvider，用于检索CommandHandler
public class CommandHandlerProvider : AbstractMessageHandlerProvider
{
    protected override bool AllowMultipleMessageHandlers => false;

    protected override Type GetMessageHandlerInterfaceGenericType()
    {
        return typeof(ICommandHandler<>);
    }
}

// 命令消费者
public class CommandConsumer
{
    private DefaultMessageProcessor _commandProcessor;

    public CommandConsumer()
    {
        var interceptors = new IInterceptor[]
        {
            // interceptor
        };
        var provider = new CommandHandlerProvider();
        provider.Initialize(interceptors, typeof(CommandTests).Assembly);
        var processingMessageHandler = new DefaultProcessingMessageHandler();
        processingMessageHandler.Initialize(provider);
        _commandProcessor = new DefaultMessageProcessor();
        _commandProcessor.Initialize(processingMessageHandler);
    }
    public void Start()
    {
        _commandProcessor.Start();
        //TODO: 接收来自MQ的消息
        var commandExecutingContext = new CommandExecutingContext();
        var processingMessage = new ProcessingMessage(commandData, string.Empty, commandExecutingContext);
        _commandProcessor.Process(processingMessage);
    }

    public void Stop()
    {
        //TODO: 停止订阅
        _commandProcessor.Stop();
    }

    internal class CommandExecutingContext : IMessageExecutingContext
    {
        private string _result;

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
            //TODO: 调用MQ的ACK
            //TODO: 将结果，反馈给Command发送者
            return Task.CompletedTask;
        }
    }
}
```

## 发布历史

### 1.0.4 （更新日期：2020/6/20）

```plain
1）去掉名字空间Interception，改为依赖Larva.DynamicProxy。
```

### 1.0.3 （更新日期：2020/6/18）

```plain
1）修复StandardInterceptor，拦截异步方法时，Dispose的调用应仍在主线程里执行，确保类似AsyncLocal变量在主线程上被释放；
2）优化StandardInterceptor，对PostProceed、ExceptionThrown、Dispose的调用，捕获异常抛出。
```

### 1.0.2 （更新日期：2020/6/14）

```plain
简化拦截器相关的代码
```

### 1.0.1 （更新日期：2020/6/13）

```plain
重命名日志相关接口和类，避免和log4net命名冲突
```

### 1.0.0 （更新日期：2020/6/13）

```plain
初始版本发布
```
