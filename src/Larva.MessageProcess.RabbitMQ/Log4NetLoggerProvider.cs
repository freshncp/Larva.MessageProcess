using System;

namespace Larva.MessageProcess.RabbitMQ
{
    public class Log4NetLoggerProvider : Larva.MessageProcess.ILoggerProvider
    {
        public ILogger GetLogger(Type source)
        {
            return new Log4NetLogger(source);
        }
    }

    internal class Log4NetLogger : Larva.MessageProcess.ILogger
    {
        private log4net.ILog _log;

        public Log4NetLogger(Type source)
        {
            _log = log4net.LogManager.GetLogger(source);
        }

        public void Trace(string content)
        {
            _log.Debug(content);
        }

        public void Debug(string content)
        {
            _log.Debug(content);
        }

        public void Info(string content)
        {
            _log.Info(content);
        }

        public void Warn(string content)
        {
            _log.Warn(content);
        }

        public void Error(string content)
        {
            _log.Error(content);
        }

        public void Error(string content, Exception exception)
        {
            _log.Error(content, exception);
        }

        public void Fatal(string content)
        {
            _log.Fatal(content);
        }

        public void Fatal(string content, Exception exception)
        {
            _log.Fatal(content, exception);
        }
    }
}