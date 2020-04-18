/******************************************************************************
 *                                                                         
 *               
 *               All rights reserved.
 *
 *****************************************************************************/

using InstrumentDriver.Core.Utility.Log;

namespace InstrumentDriver.Core.Register
{
    /// <summary>
    /// CommandField64 is a specialization of RegField64 aimed at "Command" and "Event"
    /// registers.  The difference is CommandField64 sets all other bits of the register
    /// to 0 when a Write or Value is performed  (RegField64 preserves the bits not
    /// used by the field).
    /// </summary>
    /// <remarks>
    /// Because registers are involved in literally millions of operations (register read/write)
    /// it is imperative that the implementation is as efficient as possible -- so we exclude
    /// this class from control flow obfuscation
    /// </remarks>
    [System.Reflection.ObfuscationAttribute( Feature = "controlflow=false" )]
    public class CommandField64 : RegField64
    {
        public CommandField64( string bitFieldName,
                               int startBit,
                               int sizeInBits,
                               IRegister reg )
            : base( bitFieldName, startBit, sizeInBits, reg )
        {
        }

        /// <summary>
        /// Compute the value to set the register to (either value caching or writing
        /// directly to hardware).
        /// 
        /// Unlike RegField64, this version clears all bits outside of this
        /// field (appropriate for commands and events)
        /// </summary>
        /// <param name="bits"></param>
        /// <returns></returns>
        protected override long UpdateRegField( long bits )
        {
            if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
            {
                mLogger.LogAppend(
                    new RegisterLoggingEvent( LogLevel.Fine, bits, this )
                        {
                            Operation = RegisterLoggingEvent.OperationType.BFSetVal
                        } );
            }

            return ( bits << mStartBit ) & mMask;
        }
    }
}