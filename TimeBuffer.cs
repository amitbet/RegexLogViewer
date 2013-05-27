using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
namespace LogViewer
{
    public enum TimeBuffStatus
    {
        Off,
        Waiting,
        Done,
        Error,
        Stopped
    }

    public class TimeBuffer
    {
        private Action _action = null;
        private DateTime _when = DateTime.Now;
        private TimeBuffStatus _state = TimeBuffStatus.Off;
        private Exception _problem = null;
        Thread _runnerThread = null;
        private TimeSpan _initialTimeBuffer = TimeSpan.FromSeconds(1);
        SynchronizationContext _origSyncContext = null;

        public TimeBuffer(Action act)
            : this(act, TimeSpan.FromSeconds(1))
        {
        }

        public TimeBuffer(Action act, TimeSpan timeBuffer)
        {
            _initialTimeBuffer = timeBuffer;
            _action = act;
            _origSyncContext = SynchronizationContext.Current;

            // a thread that will wait for the right time.
            CreateRunnerThread();
        }

        private void CreateRunnerThread()
        {
            _runnerThread = new Thread((ThreadStart) delegate
                {
                    _state = TimeBuffStatus.Waiting;
                    while (_state != TimeBuffStatus.Stopped)
                    {
                        //if the time for doing the action has come (or passed) - do the action
                        if (_when - DateTime.Now < TimeSpan.Zero)
                        {
                            try
                            {
                                _origSyncContext.Send((SendOrPostCallback) delegate { _action.Invoke(); }, null);

                                _state = TimeBuffStatus.Done;
                                break;
                            }
                            catch (Exception ex)
                            {
                                _problem = ex;
                                _state = TimeBuffStatus.Error;
                                break;
                            }
                        }
                        Thread.Sleep(100);
                    }
                });
        }

        /// <summary>
        /// refills time buffer (used when user operation should postpone action)
        /// </summary>
        public void Restart()
        {
            Start(_initialTimeBuffer);
        }

        /// <summary>
        /// changes the waiting end time to be now + timeFromNow, ensures the thread is running
        /// </summary>
        /// <param name="timeFromNow"></param>
        public void Start(TimeSpan timeFromNow)
        {
            _when = DateTime.Now + timeFromNow;
            Start();
        }

        /// <summary>
        /// stopps the waiting thread, no action will be performed
        /// </summary>
        public void Stop()
        {
            _state = TimeBuffStatus.Stopped;
        }

        /// <summary>
        /// starts waiting, will wait for the initial time and run action, if no changes are done until time expires.
        /// </summary>
        public void Start()
        {
            if (_runnerThread.ThreadState != ThreadState.Running && _state != TimeBuffStatus.Waiting)
            {
                CreateRunnerThread();
                _runnerThread.Start();
            }
        }
    }
}
