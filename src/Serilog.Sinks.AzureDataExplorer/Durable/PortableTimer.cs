// Copyright 2014 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Serilog.Debugging;

namespace Serilog.Sinks.AzureDataExplorer.Durable
{
    /// <summary>
    /// https://github.com/serilog/serilog-sinks-seq/blob/v4.0.0/src/Serilog.Sinks.Seq/Sinks/Seq/PortableTimer.cs
    /// The PortableTimer class is a timer implementation that executes a specified function as a recurring task after a specified time interval.
    /// The timer is implemented as a combination of the System.Threading.Timer class and the Task.Delay method, depending on the THREADING_TIMER constant.
    /// It implements the IDisposable interface, which allows the timer to be cleaned up when it's no longer needed.
    /// The timer is started using the Start method, which accepts a TimeSpan argument specifying the interval at which the task should be executed.
    /// The task is executed in an asynchronous manner using the async keyword.
    /// The OnTick method is protected by a lock to ensure that only one instance of the task is running at a time, and it checks for cancellation before executing the task.
    /// The Dispose method cancels the timer and releases any resources associated with it.
    /// </summary>
    class PortableTimer : IDisposable
    {
        readonly object m_stateLock = new object();

        readonly Func<CancellationToken, Task> m_onTick;
        readonly CancellationTokenSource m_cancel = new CancellationTokenSource();

#if THREADING_TIMER
        readonly Timer _timer;
#endif

        bool m_running;
        bool m_disposed;

        public PortableTimer(Func<CancellationToken, Task> onTick)
        {
            if (onTick == null) throw new ArgumentNullException(nameof(onTick));

            m_onTick = onTick;

#if THREADING_TIMER
            _timer = new Timer(_ => OnTick(), null, Timeout.Infinite, Timeout.Infinite);
#endif
        }

        public void Start(TimeSpan interval)
        {
            if (interval < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));

            lock (m_stateLock)
            {
                if (m_disposed)
                    throw new ObjectDisposedException(nameof(PortableTimer));

#if THREADING_TIMER
                _timer.Change(interval, Timeout.InfiniteTimeSpan);
#else
                Task.Delay(interval, m_cancel.Token)
                    .ContinueWith(
                        _ => OnTick(),
                        CancellationToken.None,
                        TaskContinuationOptions.DenyChildAttach,
                        TaskScheduler.Default);
#endif
            }
        }

        async void OnTick()
        {
            try
            {
                lock (m_stateLock)
                {
                    if (m_disposed)
                    {
                        return;
                    }

                    // There's a little bit of raciness here, but it's needed to support the
                    // current API, which allows the tick handler to reenter and set the next interval.

                    if (m_running)
                    {
                        Monitor.Wait(m_stateLock);

                        if (m_disposed)
                        {
                            return;
                        }
                    }

                    m_running = true;
                }

                if (!m_cancel.Token.IsCancellationRequested)
                {
                    await m_onTick(m_cancel.Token);
                }
            }
            catch (OperationCanceledException tcx)
            {
                SelfLog.WriteLine("The timer was canceled during invocation: {0}", tcx);
            }
            finally
            {
                lock (m_stateLock)
                {
                    m_running = false;
                    Monitor.PulseAll(m_stateLock);
                }
            }
        }

        public void Dispose()
        {
            m_cancel.Cancel();

            lock (m_stateLock)
            {
                if (m_disposed)
                {
                    return;
                }

                while (m_running)
                {
                    Monitor.Wait(m_stateLock);
                }

#if THREADING_TIMER
                _timer.Dispose();
#endif

                m_disposed = true;
            }
        }
    }
}
