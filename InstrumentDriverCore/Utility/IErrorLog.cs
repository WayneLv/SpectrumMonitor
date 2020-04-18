using System;
using System.Collections.Generic;

namespace InstrumentDriver.Core.Utility
{
    /// <summary>
    /// Interface for nested message queue ... may contain ERROR, WARNING, and INFO messages.
    /// </summary>
    public interface IErrorLog
    {
        /// <summary>
        /// Event raised when a new item is added to the ErrorLog
        /// </summary>
        event EventHandler <ErrorLogItemAddedArgs> ErrorLogItemAdded;

        /// <summary>
        /// Event raised when a child ErrorLog wants to throw an Exception.  The parent (subscriber) is
        /// responsible for determining if the exception should be cached or thrown (see ThrowException)
        /// </summary>
        event EventHandler <ErrorLogExceptionArg> ErrorLogExceptionThrown;

        /// <summary>
        /// Return an object that when locked (i.e. lock(object.ResourceLock)) will
        /// insure exclusive access to the underlying ErrorLog contents.  For example,
        /// this can be used to atomically query and clear the IErrorLog contents:
        ///     lock( log.ResourceLock ) {
        ///        list = log.ErrorList;
        ///        log.Clear();
        ///      }
        /// </summary>
        object ResourceLock
        {
            get;
        }

        /// <summary>
        /// Return true if the error log or one of its child log is not empty.
        /// </summary>
        bool IsEmpty
        {
            get;
        }

        /// <summary>
        /// Returns a shallow copy of the ErrorList, including the errors from child ErrorLogs 
        /// </summary>
        List <ErrorLogItem> ErrorList
        {
            get;
        }

        /// <summary>
        /// Clear the error log contents, including any child ErrorLogs
        /// </summary>
        void Clear();

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
        void ThrowException( Exception ex );

        /// <summary>
        /// Logs an Exception as an ERROR. 
        /// </summary>
        /// <param name="ex">Exception to be logged</param>
        void AddError( Exception ex );

        /// <summary>
        /// Logs a message string with priority ERROR. The default implementation add an
        /// InternalApplicationException with the specified message to the error queue.
        /// To specify a specific type of exception (which maps to IVI error enums) use
        /// AddError(Exception)
        /// </summary>
        /// <param name="message"></param>
        void AddError( string message );

        /// <summary>
        /// Logs a message string with priority WARNING.
        /// </summary>
        /// <param name="message"></param>
        void AddWarning( string message );

        /// <summary>
        /// Logs a message string with priority INFO.
        /// </summary>
        /// <param name="message"></param>
        void AddInformation( string message );

        /// <summary>
        /// Adds a child ErrorLog to this parent.
        /// </summary>
        /// <remarks>
        /// A parent ErrorLog object can aggregate multiple child ErrorLogs objects.  
        /// <see cref="ErrorLog.ErrorList"/> returns a list that appends the ErrorList of all children. 
        /// </remarks>
        /// <param name="child"></param>
        void AddChild( IErrorLog child );

        /// <summary>
        /// When true (default), child ErrorLog entries will be consumed/merged immediately via ErrorLogItemAdded
        /// event handler.  This maintains the time order of ErrorLogItems in the parent IErrorLog.
        /// 
        /// When false, child ErrorLog entries are not consumed until an method/property that consumes
        /// entries is called (e.g. ErrorList or ThrowFirstEntry).  This segregates/groups entries by
        /// subsystem (i.e. the parent list, after aggregating the child list, is NOT in time order).
        /// </summary>
        bool AutoMergeChildLog
        {
            get;
            set;
        }

        /// <summary>
        /// Throws the first item found with MinPriority or higher.  The item is removed from the ErrorList.
        /// InnerException will contain the original StackTrace.  If no item matches the specified priority
        /// (or the list is empty), no exception is thrown.
        /// </summary>
        /// <param name="MinPriority"></param>
        void ThrowFirstItem( ErrorPriorities MinPriority );

        /// <summary>
        /// Return the first item found with MinPriority or higher.  The item is removed from the ErrorList.
        /// If no item matches the specified priority (or the list is empty), null is returned.
        /// </summary>
        /// <param name="MinPriority"></param>
        /// <returns>entry or null</returns>
        ErrorLogItem GetFirstItem( ErrorPriorities MinPriority );


        #region "Check Engine Light" startup state properties

        /// <summary>
        /// Flag any FPGA version mismatches
        /// </summary>
        bool FpgaVersionMismatch
        {
            get;
            set;
        }

        /// <summary>
        /// Flag any OF loaded Calibration data missing.  Do not set this for user align data, etc.  
        /// </summary>
        bool OFCalDataMissingFromEE
        {
            get;
            set;
        }


        #endregion



    }

    /// <summary>
    /// The priorities for entries in IErrorLog
    /// </summary>
    public enum ErrorPriorities
    {
        Info = 0,
        Warning = 1,
        Error = 5
    }
}