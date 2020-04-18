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
    /// BitField implementation
    /// </summary>
    /// <remarks>
    /// Because registers are involved in literally millions of operations (register read/write)
    /// it is imperative that the implementation is as efficient as possible -- so we exclude
    /// this class from control flow obfuscation
    /// </remarks>
    [System.Reflection.ObfuscationAttribute( Feature = "controlflow=false" )]
    public class RegField64 : BitField
    {
        protected readonly IRegister mRegister;
        protected readonly long mMask;
        private const int REG_SIZE = 64;


        public RegField64( string bitFieldName, int startBit, int sizeInBits, IRegister reg )
            : base( bitFieldName, startBit, sizeInBits, REG_SIZE )
        {
            mRegister = reg;
            mMask = CreateMask( REG_SIZE, sizeInBits, startBit );
        }

        /// <summary>
        /// The 'resource' the implementing register will lock before performing operations.  If a client
        /// needs to perform multi-field/register operations atomically, execute the code inside:
        /// 
        /// <code>
        ///    lock( IBitField.Resource )
        ///    {
        ///       // atomic code goes here
        ///    }
        /// </code>
        /// </summary>
        /// <remarks>
        /// Normally this is the IRegister.Resource value of the register that 'owns' this IBitField
        /// instance, e.g.   get { return mRegister.Resource; }
        /// </remarks>
        public override object Resource
        {
            get
            {
                return mRegister.Resource;
            }
        }

        /// <summary>
        /// Compute the value to set the register to (either value caching or writing
        /// directly to hardware).
        /// 
        /// This version preserves all bits outside of this field (appropriate for commands and events)
        /// </summary>
        /// <param name="bits"></param>
        /// <returns></returns>
        protected virtual long UpdateRegField( long bits )
        {
            if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
            {
                mLogger.LogAppend(
                    new RegisterLoggingEvent( LogLevel.Fine, bits, this )
                        {
                            Operation = RegisterLoggingEvent.OperationType.BFSetVal
                        } );
            }

            return ( mRegister.Value64 & ~mMask ) | ( ( bits << mStartBit ) & mMask );
        }

        private long ReadBitField()
        {
            long bitFieldVal = ( mRegister.Value64 & mMask ) >> mStartBit;

            if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
            {
                mLogger.LogAppend(
                    new RegisterLoggingEvent( LogLevel.Fine, bitFieldVal, this )
                        {
                            Operation = RegisterLoggingEvent.OperationType.BFGetVal
                        } );
            }

            return bitFieldVal;
        }

        public override int Value
        {
            get
            {
                return (int)ReadBitField();
            }
            set
            {
                mRegister.Value64 = UpdateRegField( value );
            }
        }

        public override long Value64
        {
            get
            {
                return ReadBitField();
            }
            set
            {
                mRegister.Value64 = UpdateRegField( value );
            }
        }


        /// <summary>
        /// Gets the mask for the bit field within the register.
        /// </summary>
        public override long Mask
        {
            get
            {
                return mMask;
            }
        }

        /// <summary>
        /// Writes the value to HW.  If Cmd or Event register, all other bit fields are set to 0.
        /// 
        /// NOTE: for Cmd and Event registers this IS NOT the same as Value=value;Apply(); 
        /// </summary>
        /// <param name="value">value to write.</param>
        public override void Write( int value )
        {
            if( mRegister.IsCommand || mRegister.IsEvent )
            {
                mRegister.Write64( ( value << mStartBit ) & mMask ); // forces all other bits zero.
            }
            else
            {
                mRegister.Write64( UpdateRegField( value ) );
            }
        }

        /// <summary>
        /// Writes the value to HW.  If Cmd or Event register, all other bit fields are set to 0.
        /// 
        /// NOTE: for Cmd and Event registers this IS NOT the same as Value=value;Apply(); 
        /// </summary>
        /// <param name="driver">driver(port in moonstone terms) to write to.</param>
        /// <param name="value">value to write.</param>
        public override void Write( IRegDriver driver, int value )
        {
            if( mRegister.IsCommand || mRegister.IsEvent )
            {
                mRegister.Write64( driver, ( value << mStartBit ) & mMask ); // forces all other bits zero.
            }
            else
            {
                mRegister.Write64( driver, UpdateRegField( value ) );
            }
        }

        public override int Read()
        {
            // NOTE: this is different than APeX (which doesn't perform a Read() in RegField64 but does in RegField32)
            mRegister.Read64();
            return (int)ReadBitField();
        }

        /// <summary>
        /// Writes the value to HW.  If Cmd or Event register, all other bit fields are set to 0.
        /// 
        /// NOTE: for Cmd and Event registers this IS NOT the same as Value=value;Apply(); 
        /// </summary>
        /// <param name="value">value to write.</param>
        public override void Write( long value )
        {
            if( mRegister.IsCommand || mRegister.IsEvent )
            {
                mRegister.Write64( ( value << mStartBit ) & mMask ); // forces all other bits zero.
            }
            else
            {
                mRegister.Write64( UpdateRegField( value ) );
            }
        }

        /// <summary>
        /// Reads BitField value from HW.
        /// </summary>
        public override long Read64()
        {
            mRegister.Read64();
            return ReadBitField();
        }

        /// <summary>
        /// Call Apply() of the associated register.  This conditionally writes the register's
        /// value (if dirty) to hardware.
        /// </summary>
        /// <param name="forceApply"></param>
        public override void Apply( bool forceApply )
        {
            mRegister.Apply( forceApply );
        }

        /// <summary>
        /// Call Apply() of the associated register.  This conditionally writes the register's
        /// value (if dirty) to hardware using the specified driver.
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="forceApply"></param>
        public override void Apply( IRegDriver driver, bool forceApply )
        {
            mRegister.Apply( driver, forceApply );
        }

        /// <summary>
        /// Return a reference to the register this field belongs to.  THIS MAY BE NULL!!!
        /// </summary>
        public override IRegister Register
        {
            get
            {
                return mRegister;
            }
        }

        #region Register bit locking methods

        /// <summary>
        /// Prevent register bits from being modified.
        /// </summary>
        public override void LockBits()
        {
            mRegister.LockBits(mStartBit, mSizeInBits);
        }

        /// <summary>
        /// Allow register bits to be modified.
        /// </summary>
        public override void UnlockBits()
        {
            mRegister.UnlockBits(mStartBit, mSizeInBits);
        }

        #endregion Register bit locking methods
    }
}