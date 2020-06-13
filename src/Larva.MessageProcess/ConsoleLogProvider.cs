using System;
using System.IO;

namespace Larva.MessageProcess
{
    /// <summary>
    /// 控制台日志提供者
    /// </summary>
    public class ConsoleLogProvider : ILogProvider
    {
        /// <summary>
        /// 获取日志
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public ILog GetLogger(Type source)
        {
            return new ConsoleLog(source.FullName);
        }
    }

    internal class ConsoleLog : ILog
    {
        private object _locker = new object();
        private string _source;

        public ConsoleLog(string source)
        {
            _source = source;
        }

        public void Trace(string content)
        {
            lock (_locker)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} TRACE [{_source}] {content}");
                Console.ResetColor();
            }
        }

        public void Debug(string content)
        {
            lock (_locker)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} DEBUG [{_source}] {content}");
                Console.ResetColor();
            }
        }

        public void Info(string content)
        {
            lock (_locker)
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} INFO [{_source}] {content}");
            }
        }

        public void Warn(string content)
        {
            lock (_locker)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} WARN [{_source}] {content}");
                Console.ResetColor();
            }
        }

        public void Error(string content)
        {
            lock (_locker)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                var logContent = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} ERROR [{_source}] {content}";
                using (var sw = new StreamWriter(Console.OpenStandardError()))
                {
                    sw.WriteLine(logContent);
                }
                Console.WriteLine(logContent);
                Console.ResetColor();
            }
        }

        public void Error(string content, Exception exception)
        {
            lock (_locker)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                var logContent = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} ERROR [{_source}] {content}\r\n{exception.Message}\r\n{exception.StackTrace}";
                using (var sw = new StreamWriter(Console.OpenStandardError()))
                {
                    sw.WriteLine(logContent);
                }
                Console.WriteLine(logContent);
                Console.ResetColor();
            }
        }

        public void Fatal(string content)
        {
            lock (_locker)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                var logContent = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} FATAL [{_source}] {content}";
                using (var sw = new StreamWriter(Console.OpenStandardError()))
                {
                    sw.WriteLine(logContent);
                }
                Console.WriteLine(logContent);
                Console.ResetColor();
            }
        }

        public void Fatal(string content, Exception exception)
        {
            lock (_locker)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                var logContent = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} FATAL [{_source}] {content}\r\n{exception.Message}\r\n{exception.StackTrace}";
                using (var sw = new StreamWriter(Console.OpenStandardError()))
                {
                    sw.WriteLine(logContent);
                }
                Console.WriteLine(logContent);
                Console.ResetColor();
            }
        }
    }
}