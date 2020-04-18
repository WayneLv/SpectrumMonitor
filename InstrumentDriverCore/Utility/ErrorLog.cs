/******************************************************************************
 *                                                                         
 *               Copyright 2011 .
 *               All rights reserved.
 *
 *****************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace InstrumentDriver.Core.Utility
{
    /// <summary>
    /// Maintains a list <see cref="ErrorLogItem"/> entries. 
    /// Allows warnings to be logged for later reporting, and 
    /// allows Exceptions to be logged during construction for deferred reporting. 
    /// </summary>
    public class ErrorLog : IErrorLog
    {
        #region ErrorLog fields 

        private readonly object mResourceLock = new object();

        /// <summary>
        /// Default Source string for new items. 
        /// High-level description of the source of the error or warning, such as a module name. 
        /// </summary>
        private readonly string mSource;

        //  the list
        private readonly List <ErrorLogItem> mList;

        // Child logs for this parent. 
        private readonly List <IErrorLog> mChildLogs;

        #endregion

        /// <summary>
        /// Creates a new empty log. 
        /// </summary>
        /// <param name="source">Default source string for new items. </param>
        public ErrorLog( string source )
        {
            mSource = source;
            mList = new List <ErrorLogItem>();
            mChildLogs = new List <IErrorLog>();
            AutoMergeChildLog = true;

            FpgaVersionMismatch = false;
            OFCalDataMissingFromEE = false;

        }

        #region events

        /// <summary>
        /// Event raised when a new item is added to the ErrorLog
        /// </summary>
        public event EventHandler <ErrorLogItemAddedArgs> ErrorLogItemAdded;

        // forward events raised by children
        private void ChildErrorLogItemAdded( object sender, ErrorLogItemAddedArgs e )
        {
            // If the sender is an ErrorLog and AutoMergeChildLog, consume its entries
            if( AutoMergeChildLog )
            {
                IErrorLog child = sender as IErrorLog;
                if( child != null )
                {
                    lock( mResourceLock )
                    {
                        mList.AddRange( child.ErrorList );
                        child.Clear();
                    }
                }
            }
            if( ErrorLogItemAdded != null )
            {
                ErrorLogItemAdded( this, e );
            }
        }

        /// <summary>
        /// Event raised when a new item is added to the ErrorLog
        /// </summary>
        public event EventHandler <ErrorLogExceptionArg> ErrorLogExceptionThrown;

        // forward events raised by children
        private void ChildErrorLogExceptionThrown( object sender, ErrorLogExceptionArg arg )
        {
            // Simply delegate to ThrowException()
            ThrowException( arg.Exception );
        }

        #endregion

        #region public properties and methods

        /// <summary>
        /// Clear the error log contents, including any child ErrorLogs
        /// </summary>
        public void Clear()
        {
            lock( mResourceLock )
            {
                mList.Clear();
                foreach( IErrorLog child in mChildLogs )
                {
                    child.Clear();
                }

                FpgaVersionMismatch = false;
                OFCalDataMissingFromEE = false;
            }
        }

        /// <summary>
        /// If the ErrorLog is empty, throw the specified exception.
        /// 
        /// If the ErrorLog is not empty, add the specified exception to the ErrorLog as
        /// an ErrorPriorities.ERROR and throw the error/exception CheckErrorQueueException.
        /// 
        /// The intent of this is to insure the client receives errors in the order they
        /// occurred in.
        /// </summary>
        /// <param name="ex"></param>
        public void ThrowException( Exception ex )
        {
            if( ErrorLogExceptionThrown == null )
            {
                // If there is no parent/subscriber...
                if( IsEmpty )
                {
                    // Empty... just throw the exception
                    throw ex;
                }

                // Not empty ... add this to the queue and notify client via CheckErrorQueueException
                AddException( ex, ErrorPriorities.Error );

                throw new CheckErrorQueueException( ex.Message, ex );
            }

            // If there is a parent/subscriber, delegate throwing the exception to that...
            ErrorLogExceptionThrown( this, new ErrorLogExceptionArg( ex ) );
        }

        /// <summary>
        /// Return an object that when locked (i.e. lock(object.ResourceLock)) will
        /// insure exclusive access to the underlying ErrorLog contents.  For example,
        /// this can be used to atomically query and clear the IErrorLog contents:
        ///     lock( log.ResourceLock ) {
        ///        list = log.ErrorList;
        ///        log.Clear();
        ///      }
        /// </summary>
        public object ResourceLock
        {
            get
            {
                return mResourceLock;
            }
        }

        /// <summary>
        /// Return true if the error log or one of its child log is not empty.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                lock( mResourceLock )
                {
                    if( mList.Count > 0 )
                    {
                        return false;
                    }

                    // append all child lists
                    foreach( IErrorLog child in mChildLogs )
                    {
                        if( ! child.IsEmpty )
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
        }

        /// <summary>
        /// Returns a shallow copy of the ErrorList, including the errors from child ErrorLogs 
        /// </summary>
        public List <ErrorLogItem> ErrorList
        {
            get
            {
                lock( mResourceLock )
                {
                    // create a copy
                    List <ErrorLogItem> list = new List <ErrorLogItem>( mList );

                    // append any child lists
                    foreach( IErrorLog child in mChildLogs )
                    {
                        list.AddRange( child.ErrorList );
                    }

                    return list;
                }
            }
        }

        /// <summary>
        /// Logs an Exception.
        /// </summary>
        /// <param name="ex">Exception to be logged</param>
        /// <param name="priority">WARNING or ERROR</param>
        protected void AddException( Exception ex, ErrorPriorities priority )
        {
            ErrorLogItem item;
            lock( mResourceLock )
            {
                // this is a good place to print errors in debug builds
                Debug.Print( String.Format( "{0} {1}: {2}", mSource, priority, ex.Message ) );

                item = new ErrorLogItem { Priority = priority, Source = mSource, Exception = ex };
                mList.Add( item );
            }

            // if anyone is listening, raise an event.
            if( ErrorLogItemAdded != null )
            {
                ErrorLogItemAdded( this, new ErrorLogItemAddedArgs( item ) );
            }
        }

        /// <summary>
        /// Logs an Exception as an ERROR. 
        /// </summary>
        /// <param name="ex">Exception to be logged</param>
        public void AddError( Exception ex )
        {
            AddException( ex, ErrorPriorities.Error );
        }

        /// <summary>
        /// Logs a message string with priority ERROR (uses InternalApplicationException)
        /// </summary>
        /// <param name="message"></param>
        public void AddError( string message )
        {
            // wrap the message in a new exception object
            Exception ex = new InternalApplicationException( message );

            AddException( ex, ErrorPriorities.Error );
        }

        /// <summary>
        /// Logs a message string with priority WARNING.
        /// </summary>
        /// <param name="message"></param>
        public void AddWarning( string message )
        {
            // wrap the message in a new exception object and throw it so we have a StackTrace
            try
            {
                throw new WarningException( message );
            }
            catch( Exception ex )
            {
                AddException( ex, ErrorPriorities.Warning );
            }
        }

        /// <summary>
        /// Logs a message string with priority INFO.
        /// </summary>
        /// <param name="message"></param>
        public void AddInformation( string message )
        {
            // wrap the message in a new exception object and throw it so we have a StackTrace
            try
            {
                throw new InformationException( message );
            }
            catch( Exception ex )
            {
                AddException( ex, ErrorPriorities.Info );
            }
        }

        /// <summary>
        /// Adds a child ErrorLog to this parent.
        /// 
        /// NOTE: if 'child' is null, this is a NOP
        /// </summary>
        /// <remarks>
        /// A parent ErrorLog object can aggregate multiple child ErrorLogs objects.  
        /// <see cref="ErrorList"/> returns a list that appends the ErrorList of all children. 
        /// </remarks>
        /// <param name="child">child error log, may be null (if null, ignored)</param>
        public void AddChild( IErrorLog child )
        {
            // Sanity checks...
            if( child != null && ! ReferenceEquals( child, this ) )
            {
                lock( mResourceLock )
                {
                    mChildLogs.Add( child );

                    // subscribe to events
                    child.ErrorLogItemAdded += ChildErrorLogItemAdded;
                    child.ErrorLogExceptionThrown += ChildErrorLogExceptionThrown;
                }
            }
        }

        /// <summary>
        /// When true (default), child ErrorLog entries will be consumed/merged immediately via ErrorLogItemAdded
        /// event handler.  This maintains the time order of ErrorLogItems in the parent IErrorLog.
        /// 
        /// When false, child ErrorLog entries are not consumed until an method/property that consumes
        /// entries is called (e.g. ErrorList or ThrowFirstEntry).  This segregates/groups entries by
        /// subsystem (i.e. the parent list, after aggregating the child list, is NOT in time order).
        /// </summary>
        public bool AutoMergeChildLog
        {
            get;
            set;
        }

        /// <summary>
        /// Throws the first item found with MinPriority or higher.  The item is removed from the ErrorList.
        /// InnerException will contain the original StackTrace.  If no item matches the specified priority
        /// (or the list is empty), no exception is thrown.
        /// </summary>
        /// <param name="minPriority"></param>
        public void ThrowFirstItem( ErrorPriorities minPriority )
        {
            ErrorLogItem item = GetFirstItem( minPriority );
            if( item != null )
            {
                // Don't directly throw the original item.Exception because throwing will 
                // overwrite its StackTrace.  Instead, wrap it in a new exception 
                // of the same type, with the original exception as InnerException.
                Exception ex = (Exception)Activator.CreateInstance(
                    item.Exception.GetType(),
                    new object[] { item.Message, item.Exception } );
                // bye bye...
                throw ex;
            }
        }

        /// <summary>
        /// Return the first item found with MinPriority or higher.  The item is removed from the ErrorList.
        /// If no item matches the specified priority (or the list is empty), null is returned.
        /// </summary>
        /// <param name="minPriority"></param>
        /// <returns>entry or null</returns>
        public ErrorLogItem GetFirstItem( ErrorPriorities minPriority )
        {
            lock( mResourceLock )
            {
                // Don't use 'ErrorList' -- we can't remove the item...
                foreach( ErrorLogItem item in mList )
                {
                    if( (int)item.Priority >= (int)minPriority )
                    {
                        // Remove the item from the ErrorList
                        mList.Remove( item );
                        return item;
                    }
                }

                // If not in the local list, check the children
                foreach( IErrorLog child in mChildLogs )
                {
                    ErrorLogItem item = child.GetFirstItem( minPriority );
                    if( item != null )
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        #endregion


        /// <summary>
        /// Flag any FPGA version mismatches
        /// </summary>
        public bool FpgaVersionMismatch
        {
            get;
            set;
        }

        /// <summary>
        /// Flag any OF loaded Calibration data missing.  Do not set this for user align data, etc.  
        /// </summary>
        public bool OFCalDataMissingFromEE
        {
            get;
            set;
        }
    }
}