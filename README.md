# Larva.MessageProcess

![.NET Core](https://github.com/freshncp/Larva.MessageProcess/workflows/.NET%20Core/badge.svg)

消息处理框架。不同业务键并行处理，相同业务键串行处理，思路来源于 [ENode](http://github.com/tangxuehua/enode)。

## 功能列表

- 支持自定义消息类型名

- 支持多个订阅者，订阅者之间并行消费

- 支持多个消息处理器，消息处理器之间串行处理，可设置优先级

- 支持消息执行结果返回

- 消息可以没有消息处理器，使用者可以自行决定是否ACK

- 支持消息组概念，即将一组相同业务键的消息打包成一个消息组进行发送和消费

- 支持设置消息处理失败时是否继续消费相同业务键的消息（默认此类消息不会继续处理）

- 支持消息处理失败后定时重试

- 支持消息处理器的拦截

- 内置用于幂等的消息处理器拦截器，幂等存储需自定义实现

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
    private readonly IProcessingMessageMailboxProvider _mailboxProvider;
    private readonly ILogger _logger;
    private CommandHandlerProvider _commandHandlerProvider;
    private DefaultMessageProcessor _commandProcessor;
    private Consumer _consumer;

    public CommandConsumer()
    {
        _mailboxProvider = new DefaultProcessingMessageMailboxProvider(new DefaultProcessingMessageHandler());
        _logger = LoggerManager.GetLogger(typeof(CommandConsumer));
    }

    public void Initialize(ConsumerSettings consumerSettings, string topic, int queueCount, int retryIntervalSeconds, IInterceptor[] interceptors, params Assembly[] assemblies)
    {
        _consumer = new Consumer(consumerSettings);
        _consumer.Subscribe(topic, queueCount);

        _commandHandlerProvider = new CommandHandlerProvider();
        _commandHandlerProvider.Initialize(interceptors, assemblies);
        _commandProcessor = new DefaultMessageProcessor(string.Empty, _commandHandlerProvider, _mailboxProvider, true, retryIntervalSeconds);
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
                var processingCommand = new ProcessingMessage(command, new CommandExecutingContext(_logger, e.Context), commandMessage.ExtraDatas);
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

    private class CommandExecutingContext : IMessageExecutingContext
    {
        private readonly ILogger _logger;
        private readonly IMessageTransportationContext _transportationContext;
        private string _result;

        public CommandExecutingContext(ILogger logger, IMessageTransportationContext transportationContext)
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
                _logger.Info($"Result={messageResult}\r\nRawMessage={JsonConvert.SerializeObject(messageResult.RawMessage)}");
            }
            else
            {
                _logger.Error($"Result={messageResult}\r\nRawMessage={JsonConvert.SerializeObject(messageResult.RawMessage)}\r\n{messageResult.StackTrace}");
            }
            return Task.CompletedTask;
        }
    }
}
```

## 发布历史

### 1.2.8 （更新日期：2020/9/14）

```plain
1）DefaultMessageProcessor 增加Mailbox的淘汰策略，默认为LFU策略，目的是降低Mailbox的内存消耗；
2）内置 AutoIdempotentInterceptor 拦截器，并调整 IAutoIdempotentStore 接口，不再区分 multipleMessageHandler。
```

### 1.2.7 （更新日期：2020/8/19）

```plain
1）Mailbox 当传入参数continueWhenHandleFail=true时，消息重试时间间隔bug修复。
```

### 1.2.6 （更新日期：2020/8/13）

```plain
1）retryIntervalSeconds 如果设置为-1，表示不重试；
2）Mailbox 执行Stop后，对于Task.Delay这类异步处理时间长的，传入CancellationToken以尽快停止。
```

### 1.2.5 （更新日期：2020/8/13）

```plain
1）修复Mailbox处理消息时，如果continueWhenHandleFail=true，当发生错误后，MessageProcessor将无法停止的bug；
2）简化处理问题消息的逻辑。
```

### 1.2.4 （更新日期：2020/8/12）

```plain
1）修复Mailbox处理消息的bug。
```

### 1.2.3 （更新日期：2020/8/12）

```plain
1）ProcessingMessage不再处理Mailbox中移除消息的操作，由Mailbox自行完成，在发生消费进度往下移动时进行；
2）移除了无用的代码。
```

### 1.2.2 （更新日期：2020/8/12）

```plain
1）修复不存在问题消息，当Mailbox触发Inactive后，mailbox从MessageProcessor中移除，但本身未被停止，导致一直占用内存和cpu的bug；
2）mailboxTimeoutSeconds 由默认1天，改为4小时；
3）优化处理问题消息、处理消息的抢占问题。
```

### 1.2.1 （更新日期：2020/8/10）

```plain
1）代码重构，更好支持基于接口的依赖注入，目前支持 IProcessingMessageMailboxProvider、IProcessingMessageHandler；
2）修复存在问题消息时，停止DefaultMessageProcessor陷入死循环的bug。
```

### 1.2.0 （更新日期：2020/8/9）

```plain
1）支持消息消费失败自动重试，重试间隔以秒计（失败不一定是业务代码bug造成）；
2）ProcessingMessage的属性ContinueWhenHandleFail、MessageSubscriber，转为IProcessingMessageHandler、IMessageProcessor的初始化参数。
```

### 1.1.1 （更新日期：2020/7/12）

```plain
1）移除参数的params修饰，此可能在传入null时，导致非预期结果；
2）调整MessageProcessor、Mailbox，使支持通过ObjectContainer可替换其他Mailbox实现类；
3）调整ObjectContainer，如果自定义解析器解析失败，则使用默认解析；
4）重命名名字空间MailBoxes为Mailboxes；
5）IAutoIdempotentStore 增加方法WaitForSave，用于服务停止时等待保存完成，从而降低不幂等概率；
6）Mailbox增加了校验，ProcessMessagesAsync的Task启动选项，改为默认。
```

### 1.1.0 （更新日期：2020/7/8）

```plain
1）升级Larva.DynamicProxy；
2）Handlers 重命名为 Handling；
3）新增幂等支持，通过标准拦截器的PreProceed，使用实现了IAutoIdempotentStore的幂等存储，来判断是否已处理过，已处理过的抛出DuplicateMessageHandlingException异常，在PostProceed中保存已处理。
```

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
