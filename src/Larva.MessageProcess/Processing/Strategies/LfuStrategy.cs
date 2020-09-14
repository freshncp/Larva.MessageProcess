using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Larva.MessageProcess.Processing.Strategies
{
    /// <summary>
    /// LFU策略
    /// </summary>
    public class LfuStrategy : IEliminationStrategy
    {
        private ConcurrentDictionary<string, int> _frontendFrequenceDict;
        private ConcurrentDictionary<string, int> _backendFrequenceDict;
        private readonly Timer _statTimer;
        private volatile int _isRunning = 0;

        /// <summary>
        /// LFU策略
        /// </summary>
        /// <param name="statPeriodMinutes">统计周期分钟数</param>
        /// <param name="minFrequencePerMinute">每分钟最低频率</param>
        public LfuStrategy(int statPeriodMinutes = 1, int minFrequencePerMinute = 6)
        {
            if (statPeriodMinutes < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(statPeriodMinutes), "Must great or equal than 1");
            }
            if (minFrequencePerMinute < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(minFrequencePerMinute), "Must great or equal than 1");
            }
            StatPeriodMinutes = statPeriodMinutes;
            MinFrequencePerMinute = minFrequencePerMinute;
            _frontendFrequenceDict = new ConcurrentDictionary<string, int>();
            _backendFrequenceDict = new ConcurrentDictionary<string, int>();
            _statTimer = new Timer(Statistic);
        }

        /// <summary>
        /// 统计周期分钟数
        /// </summary>
        public int StatPeriodMinutes { get; }

        /// <summary>
        /// 每分钟最低频率
        /// </summary>
        public int MinFrequencePerMinute { get; }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning => _isRunning == 1;

        /// <summary>
        /// 已淘汰事件
        /// </summary>
        public event EventHandler<KnockedOutEventArgs> OnKnockedOut;

        /// <summary>
        /// 启动
        /// </summary>
        public void Start()
        {
            if (Interlocked.CompareExchange(ref _isRunning, 1, 0) == 0)
            {
                _statTimer.Change(TimeSpan.FromMinutes(StatPeriodMinutes), TimeSpan.FromMinutes(StatPeriodMinutes));
            }
        }

        /// <summary>
        /// 停止
        /// </summary>
        public void Stop()
        {
            if (Interlocked.CompareExchange(ref _isRunning, 0, 1) == 1)
            {
                _statTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _frontendFrequenceDict.Clear();
                _backendFrequenceDict.Clear();
            }
        }

        /// <summary>
        /// 添加Key
        /// </summary>
        /// <param name="key"></param>
        public void AddKey(string key)
        {
            if (IsRunning)
            {
                _frontendFrequenceDict.AddOrUpdate(key, 1, (k, originVal) => ++originVal);
            }
        }

        private void Statistic(object state)
        {
            _backendFrequenceDict = _frontendFrequenceDict;
            _frontendFrequenceDict = new ConcurrentDictionary<string, int>();
            if (_backendFrequenceDict.Count == 0)
            {
                return;
            }

            var lowFrequenceKeys = _backendFrequenceDict.Where(w => w.Value < MinFrequencePerMinute).Select(s => s.Key).ToArray();
            if (lowFrequenceKeys != null && lowFrequenceKeys.Length > 0)
            {
                try
                {
                    OnKnockedOut?.Invoke(this, new KnockedOutEventArgs(lowFrequenceKeys));
                }
                catch { }
            }
            _backendFrequenceDict.Clear();
        }
    }
}
