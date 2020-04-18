using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace InstrumentDriver.Core.Utility
{
	/// <summary>
	/// ThreadWrapper aggregates a Thread object and keeps track of which ThreadWrapper
	/// objects are still active -- which does not appear to be possible with Thread
	/// objects (i.e. which Thread execute methods have not returned yet).
	/// 
	/// ThreadWrapper also aggregates some signaling/event objects and methods that are
	/// commonly used to control the thread.
	/// </summary>
	public class ThreadWrapper
	{
		#region member variables

		private static readonly List <ThreadWrapper> mRunning = new List <ThreadWrapper>( 10 );

		private readonly Thread mThread;
		private readonly ThreadStart mStart;
		private readonly ParameterizedThreadStart mParameterizedStart;
        private readonly ManualResetEvent mExitEvent = new ManualResetEvent( false );
        private readonly ManualResetEvent mFinishedEvent = new ManualResetEvent( true );
        private readonly AutoResetEvent mSignalEvent = new AutoResetEvent( false );

		#endregion member variables

		#region Thread equivalent methods

		/// <summary>
		/// Construct a ThreadWrapper which aggregates a Thread object in order to keep track of
		/// which threads are running.  From the client view, ThreadWrapper exposes the same
		/// interface as Thread.
		/// </summary>
		/// <param name="start"></param>
		public ThreadWrapper( ThreadStart start )
		{
			mStart = start;
			mThread = new Thread( WrappedStart );
		}

		/// <summary>
		/// Construct a ThreadWrapper which aggregates a Thread object in order to keep track of
		/// which threads are running.  From the client view, ThreadWrapper exposes the same
		/// interface as Thread.
		/// </summary>
		/// <param name="start"></param>
		public ThreadWrapper( ParameterizedThreadStart start )
		{
			mParameterizedStart = start;
			mThread = new Thread( WrappedParameterizedStart );
		}

		/// <summary>
		/// Start the aggregated Thread.  This method actually will invoke a private method,
		/// WrappedStart, which will call the client specified start method.
		/// </summary>
		public void Start( object arg )
		{
		    mFinishedEvent.Reset();
			mThread.Start( arg );
		}


		/// <summary>
		/// Start the aggregated Thread.  This method actually will invoke a private method,
		/// WrappedStart, which will call the client specified start method.
		/// </summary>
		public void Start()
		{
		    mFinishedEvent.Reset();
			mThread.Start();
		}

		/// <summary>
		/// Sets the apartment state of the thread before it is started
		/// </summary>
		/// <param name="state"></param>
		public void SetApartmentState( ApartmentState state )
		{
			mThread.SetApartmentState( state );
		}

        /// <summary>
        /// Return a reference to the actual Thread instance.
        /// 
        /// NOTE: accessing some thread methods directly may "confuse" booking -- e.g. calling Thread.Abort()
        ///       will not update the running count (which is updated by calling Abort())
        /// </summary>
	    public Thread Thread
	    {
	        get
	        {
	            return mThread;
	        }
	    }

        /// <summary>
        /// Set/get the thread priority.
        /// </summary>
	    public ThreadPriority Priority
	    {
	        get
	        {
	            return mThread.Priority;
	        }
            set
            {
                mThread.Priority = value;
            }
	    }

		/// <summary>
		/// Get/set name of thread
		/// </summary>
		public string Name
		{
			[DebuggerStepThrough]
			get
			{
				return mThread.Name;
			}
			[DebuggerStepThrough]
			set
			{
                mThread.Name = value;
			}
		}

		/// <summary>
		/// Gets a value indicating the execution status of the current thread.
		/// </summary>
		public bool IsAlive
		{
			get
			{
				return mThread.IsAlive;
			}
		}

		/// <summary>
		/// Raises a System.Threading.ThreadAbortException in the thread on which it is invoked, to begin
		/// the process of terminating the thread.  Calling this method usually terminates the thread.
		/// </summary>
		public void Abort()
		{
			mThread.Abort();

            // Since the thread should be dead, remove from running thread collection
            lock( mRunning )
			{
				mRunning.Remove( this );
			}

		}

		/// <summary>
		/// Blocks the calling thread until a thread terminates or until the specified time elapses, while
		/// continuing to perform standard COM and SendMessage pumping.
		/// </summary>
		public void Join()
		{
            if( mThread.IsAlive )
            {
                mThread.Join();
            }
		}

		/// <summary>
		/// Blocks the calling thread until a thread terminates or until the specified time elapses, while
		/// continuing to perform standard COM and SendMessage pumping.
		/// </summary>
		public bool Join( int timeout )
		{
			return ( mThread.IsAlive ) ? mThread.Join( timeout ) : true;
		}

		/// <summary>
		/// Blocks the calling thread until a thread terminates or until the specified time elapses, while
		/// continuing to perform standard COM and SendMessage pumping.
		/// </summary>
		public bool Join( TimeSpan timeout )
		{
			return ( mThread.IsAlive ) ? mThread.Join( timeout ) : true;
		}

		#endregion equivalent methods

		#region other stuff

		/// <summary>
		/// Wrap the start method for the aggregated Thread.  This method uses the "thread catalog"
		/// to keep track of which threads are running.  It also insures that any Exception that
		/// occurs in the start method will be caught and reported.
		/// </summary>
		private void WrappedStart()
		{
			lock( mRunning )
			{
				mRunning.Add( this );
			}

			try
			{
				mStart.Invoke();
                Finished();
			}
			catch( Exception ex )
			{
				string name = string.IsNullOrEmpty( mThread.Name ) ? "Unnamed" : mThread.Name;
				string message = string.Format( "Error in thread {0}:{1}", name, ex.Message );
                // DON'T throw this!  Since this is the start method of a thread there isn't
                // anything to catch it...
				//throw new ApplicationException( message, ex );
                Debug.Print( "{0}\n{1}\n", message, ex.StackTrace );
			}

			lock( mRunning )
			{
				mRunning.Remove( this );
			}
		}


		/// <summary>
		/// Wrap the start method for the aggregated Thread.  This method uses the "thread catalog"
		/// to keep track of which threads are running.  It also insures that any Exception that
		/// occurs in the start method will be caught and reported.
		/// </summary>
		/// <param name="arg"></param>
		private void WrappedParameterizedStart( object arg )
		{
			lock( mRunning )
			{
				mRunning.Add( this );
			}

			try
			{
				mParameterizedStart.Invoke( arg );
                Finished();
			}
			catch( Exception ex )
			{
				string name = string.IsNullOrEmpty( mThread.Name ) ? "Unnamed" : mThread.Name;
				string message = string.Format( "Error in thread {0}:{1}", name, ex.Message );
                // DON'T throw this!  Since this is the start method of a thread there isn't
                // anything to catch it...
				//throw new ApplicationException( message, ex );
                Debug.Print( "{0}\n{1}\n", message, ex.StackTrace );
			}

			lock( mRunning )
			{
				mRunning.Remove( this );
			}
		}

		#endregion other stuff

		#region thread management

		/// <summary>
		/// Return the number of still running ThreadWrapper objects
		/// </summary>
		/// <returns></returns>
		public static int GetRunningCount()
		{
			lock( mRunning )
			{
				return mRunning.Count;
			}
		}

		/// <summary>
		/// Call Thread.Abort() on all running ThreadWrapper objects.
		/// </summary>
		public static void KillRunningThreads()
		{
			lock( mRunning )
			{
				foreach( ThreadWrapper thread in mRunning )
				{
					thread.mThread.Abort();
				}
                mRunning.Clear();
			}
		}

		#endregion thread management

        #region thread helpers
        // --------------------------------------------------------------------
        // Thread helpers are useful for interacting with a worker thread.
        // This methods (and the corresponding events) do not need to be used.
        // Typical usage is something like:
        // {
        //    ...
        //    ThreadWrapper thread = new ThreadWrapper( DoWork );
        //    thread.Start()
        //    ...
        //    while( ... ) {  ... thread.Signal() ... }
        //    ...
        //    thread.Exit( timeout )
        // }
        // void DoWork()
        // {
        //    ...
        //    try {
        //       while( WaitForSignal( INFINITE ) ) {
        //          ... do something ...
        //       }
        //    } catch( Exception ex ) { ... }
        //    ...
        // }
        // --------------------------------------------------------------------
        public enum ThreadWrapperEventEnum
        {
            [Description("Request Timed Out")] RequestTimeout = -1,
            [Description("Exit Requested")] ExitRequest = 0,
            [Description("Signal Requested")] SignalRequest = 1
        }


        /// <summary>
        /// Signal the thread to "do something".  If the thread calls TBD it will
        /// receive this event.
        /// </summary>
        public void Signal()
        {
            mSignalEvent.Set();
        }

        /// <summary>
        /// Wait for something to "signal" the thread (i.e. call Signal()) or
        /// request an exit (i.e. call Exit())
        /// </summary>
        /// <returns>exit, signal</returns>
        public ThreadWrapperEventEnum WaitForSignal()
        {
            WaitHandle[] events = { mExitEvent, mSignalEvent };
            switch( WaitHandle.WaitAny( events ) )
            {
                case 0:
                    return ThreadWrapperEventEnum.ExitRequest;
                case 1:
                    return ThreadWrapperEventEnum.SignalRequest;
                default:
                    return ThreadWrapperEventEnum.RequestTimeout;
            }
        }

        /// <summary>
        /// Wait up to timeoutMilliseconds for something to "signal" the thread (i.e.
        /// call Signal()) or request an exit (i.e. call Exit())
        /// </summary>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns>exit, signal, timeout</returns>
        public ThreadWrapperEventEnum WaitForSignal( int timeoutMilliseconds )
        {
            WaitHandle[] events = { mExitEvent, mSignalEvent };
            switch( WaitHandle.WaitAny( events, timeoutMilliseconds ) )
            {
                case 0:
                    return ThreadWrapperEventEnum.ExitRequest;
                case 1:
                    return ThreadWrapperEventEnum.SignalRequest;
                default:
                    return ThreadWrapperEventEnum.RequestTimeout;
            }
        }

        /// <summary>
        /// Poll the exit event ...
        /// </summary>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns>true if exit has been requested</returns>
        public bool IsExiting( int timeoutMilliseconds )
        {
            return mExitEvent.WaitOne( timeoutMilliseconds );
        }

        /// <summary>
        /// Request the thread to exit and wait up to timeoutMilliseconds for it
        /// to actually exit using WaitUntilFinished()
        /// </summary>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns>true if thread finished, false if timed out</returns>
        public bool Exit( int timeoutMilliseconds )
        {
            // Set the exit event...
            mExitEvent.Set();

            // Delegate the wait to Join...
            return WaitUntilFinished( timeoutMilliseconds );
        }

        /// <summary>
        /// Set the finished event looked at by WaitUntilFinished().  Normally a thread worker
        /// will call Finished before exiting as an alternative to counting on the behavior of
        /// thread / Join
        /// </summary>
        public void Finished()
        {
            mFinishedEvent.Set();
        }

        /// <summary>
        /// Wait until the thread has exited. The worker thread must call Finished()
        /// </summary>
        /// <param name="timeoutMilliseconds">maximum time to wait in milliseconds</param>
        /// <returns>true if thread finished, false if timed out</returns>
        public bool WaitUntilFinished( int timeoutMilliseconds )
        {
            return mFinishedEvent.WaitOne( timeoutMilliseconds );
        }

        #endregion thread helpers
    }
}