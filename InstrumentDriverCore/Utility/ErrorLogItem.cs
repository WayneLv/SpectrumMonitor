/******************************************************************************
 *                                                                         
 *                .
 *               All rights reserved.
 *
 *****************************************************************************/
using System;
using System.Runtime.InteropServices;

namespace InstrumentDriver.Core.Utility
{
    /// <summary>
    /// Data-only class of fields for one ErrorLog entry
    /// </summary>
    public class ErrorLogItem
    {
        public ErrorLogItem()
        {
            TimeStamp = DateTime.Now;
        }

        /// <summary>
        /// Time when the item was added to the log
        /// </summary>
        public DateTime TimeStamp
        {
            get;
            set;
        }

        /// <summary>
        /// Category and priority of the item
        /// </summary>
        public ErrorPriorities Priority
        {
            get;
            set;
        }

        /// <summary>
        /// High-level description of the source of the error or warning, such as 
        /// a module name. 
        /// </summary>
        public string Source
        {
            get;
            set;
        }


        /// <summary>
        /// Exception object holds details of the error or warning.
        /// Exception.Message describes the error or warning.
        /// Exception.
        /// Other Exception properties (e.g. StackTrace) may be null if the exception
        /// has not been thrown and caught. 
        /// </summary>
        public Exception Exception
        {
            get;
            set;
        }


        public string Message
        {
            get
            {
                return ( Exception != null ) ? Exception.Message : "";
            }
        }

        public int ErrorCode
        {
            get
            {
                COMException ex = Exception as COMException;
                return ( ex == null ) ? -1 /* TODO: ? */ : ex.ErrorCode;
            }
        }
    }
}