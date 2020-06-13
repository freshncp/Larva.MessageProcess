using System;
using System.Threading;
using Xunit;

namespace Larva.MessageProcess.RabbitMQ.Tests
{
    public class LockerTests
    {
        private SpinLock locker = new SpinLock(true);

        [Fact]
        public void TestSpinLock()
        {
            var lockToken = false;
            locker.Enter(ref lockToken);

            new Thread(() =>
            {
                var lockToken2 = false;
                Console.WriteLine("Try Get locker.");
                locker.TryEnter(ref lockToken2);
                if (lockToken2)
                {
                    Console.WriteLine("Try get locker success.");
                }
                else
                {
                    Console.WriteLine("Try get locker fail.");
                }
                locker.Enter(ref lockToken2);
                if (lockToken2)
                {
                    Console.WriteLine("Get locker success.");
                }
                else
                {
                    Console.WriteLine("Get locker fail.");
                }
            }).Start();

            Thread.Sleep(1000);
            locker.Exit();
            Thread.Sleep(1000);
        }
    }
}