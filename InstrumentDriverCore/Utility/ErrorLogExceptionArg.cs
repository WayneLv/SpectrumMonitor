/******************************************************************************
 *                                                                         
 *               Copyright 2012 .
 *               All rights reserved.
 *
 *****************************************************************************/
using System;

namespace InstrumentDriver.Core.Utility
{
    /// <summary>
    /// Container class for event arguments for IErrorLog.ErrorLogThrowException.
    /// </summary>
    public class ErrorLogExceptionArg : EventArgs
    {
        public ErrorLogExceptionArg( Exception ex )
        {
            Exception = ex;
        }

        public Exception Exception
        {
            get;
            private set;
        }
    }
}
