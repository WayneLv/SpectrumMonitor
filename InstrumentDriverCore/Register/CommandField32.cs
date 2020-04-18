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
    /// CommandField32 is a specialization of RegField32 aimed at "Command" and "Event"
    /// registers.  The difference is CommandField32 sets all other bits of the register
    /// to 0 when a Write or Value is performed  (RegField32 preserves the bits not
    /// used by the field).
    /// </summary>
    /// <remarks>
    /// Because registers are involved in literally millions of operations (register read/write)
    /// it is imperative that the implementation is as efficient as possible -- so we exclude
    /// this class from control flow obfuscation
    /// </remarks>
    [System.Reflection.ObfuscationAttribute( Feature = "controlflow=false" )]
    public class CommandField32 : RegField32
    {
        public CommandField32( string bitFieldName,
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
        /// Unlike RegField32, this version clears all bits outside of this
        /// field (appropriate for commands and events)
        /// </summary>
        /// <param name="bitsToSet"></param>
        /// <returns></returns>
        protected override int UpdateRegField( int bitsToSet )
        {
            if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
            {
                mLogger.LogAppend(
                    new RegisterLoggingEvent( LogLevel.Fine, bitsToSet, this )
                        {
                            Operation = RegisterLoggingEvent.OperationType.BFSetVal
                        } );
            }

            return ( bitsToSet << mStartBit ) & mMask;
        }
    }
}