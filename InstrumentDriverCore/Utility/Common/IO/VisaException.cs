/******************************************************************************
 *                                                                         
 *               Copyright 2011 .
 *               All rights reserved.
 *
 *****************************************************************************/
using System;

namespace InstrumentDriver.Core.Common.IO
{
    public class VisaException : Exception
    {
        public int ErrorCode
        {
            get;
            set;
        }

        public VisaException()
        {
        }

        public VisaException( string msg ) : base( msg )
        {
        }

        public VisaException( string msg, Exception inner ) : base( msg, inner )
        {
        }

        public VisaException( int errorCode )
            : base( Enum.IsDefined( typeof( StatusCodes ), errorCode )
                        ? String.Format( "Visa error: '{0}'.", Enum.GetName( typeof( StatusCodes ), errorCode ) )
                        : String.Format( "Visa error: '{0}'.", errorCode ) )
        {
            ErrorCode = errorCode;
        }
    }

    public static class AgVisa32Exception
    {
        public static void Throw( int errorCode )
        {
            throw new VisaException( errorCode ); // constructor will create nice .Message
        }
    }

}