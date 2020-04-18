using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading;
using System.Windows.Threading;

namespace SpectrumMonitor.ViewModel
{
    public abstract class AbstractModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Allow ViewModel to marshall to the appropriate thread when needed
        /// </summary>
        public static DispatcherObject ViewDispatcher { get; set; } = null;

        protected internal void NotifyPropertyChanged<T>(Expression<Func<T>> propertyAccessor, T value, TraceLevel debugLevel = TraceLevel.Verbose)
        {
            string name = String.Empty;
            try
            {
                MemberExpression mExpress = (MemberExpression)propertyAccessor.Body;
                name = mExpress.Member.Name;
                Debug.Write($"PropertyChanged: {name}={value}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex.GetType().Name} in {DebugName}.NotifyPropertyChanged(): {ex.Message}");
            }

            if (ViewDispatcher == null)
            {
                Debug.Fail(GetType().Name + ".ViewDispatcher is null!");
            }
            if (PropertyChanged == null)
            {
                return;
            }

            try
            {
                if (InvokeRequired)
                {
                    ViewDispatcher.Dispatcher.BeginInvoke(new FireChangedDelegate(FireChanged), new object[] { name });
                }
                else
                    FireChanged(name);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex.GetType().Name} in {DebugName}.NotifyPropertyChanged(): {ex.Message}");
            }
        }

        protected internal void NotifyPropertyChanged<T>(Expression<Func<T>> propertyAccessor)
        {
            string name = String.Empty;
            try
            {
                MemberExpression mExpress = (MemberExpression)propertyAccessor.Body;
                name = mExpress.Member.Name;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex.GetType().Name} in {DebugName}.NotifyPropertyChanged(): {ex.Message}");
            }

            if (ViewDispatcher == null)
            {
                Debug.Fail(GetType().Name + ".ViewDispatcher is null!");
            }
            if (PropertyChanged == null)
            {
                return;
            }

            try
            {
                if (InvokeRequired)
                {
                    ViewDispatcher.Dispatcher.BeginInvoke(new FireChangedDelegate(FireChanged), new object[] { name });
                }
                else
                    FireChanged(name);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex.GetType().Name} in {DebugName}.NotifyPropertyChanged(): {ex.Message}");
            }
        }

        private delegate void FireChangedDelegate(string name);

        protected void FireChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected bool InvokeRequired
        {
            get
            {
                Thread curThread = Thread.CurrentThread;
                if (ViewDispatcher != null)
                    return curThread.ManagedThreadId != ViewDispatcher.Dispatcher.Thread.ManagedThreadId;
                Debug.Fail(GetType().Name + ".ViewDispatcher is null!");
                return true;
            }
        }

        #region DoEvents
        protected void DoEvents()
        {
            try
            {
                if ((ViewDispatcher == null) || (ViewDispatcher.Dispatcher.Thread.ManagedThreadId != Thread.CurrentThread.ManagedThreadId))
                    return;

                DispatcherFrame frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                    new DispatcherOperationCallback(ExitFrame), frame);
                Dispatcher.PushFrame(frame);
            }
            catch (Exception ex)
            {
                Debug.Write($"{ex.GetType().Name} in DoEvents(): {ex.Message}");
            }
        }

        protected object ExitFrame(object frame)
        {
            ((DispatcherFrame)frame).Continue = false;

            return null;
        }
        #endregion DoEvents

        public virtual string DebugName => GetType().Name;

        #endregion

    }
}
