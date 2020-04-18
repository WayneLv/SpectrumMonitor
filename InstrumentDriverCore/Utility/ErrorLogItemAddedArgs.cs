/******************************************************************************
 *                                                                         
 *               Copyright 2011 .
 *               All rights reserved.
 *
 *****************************************************************************/
using System;

namespace InstrumentDriver.Core.Utility
{
    /// <summary>
    /// Container class for event arguments for IErrorLog.ErrorLogItemAdded.
    /// </summary>
    public class ErrorLogItemAddedArgs : EventArgs
    {
        public ErrorLogItemAddedArgs( ErrorLogItem item )
        {
            Item = item;
        }

        public ErrorLogItem Item
        {
            get;
            private set;
        }
    }
}