using System;

namespace Larva.MessageProcess.RabbitMQ
{
    public class Log4NetLogProvider : Larva.MessageProcess.ILogProvider
    {
        public ILog GetLogger(Type source)
        {
            return new Log4NetLog(source);
        }
    }

    internal class Log4NetLog : Larva.MessageProcess.ILog
    {
        private log4net.ILog _log;

        public Log4NetLog(Type source)
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