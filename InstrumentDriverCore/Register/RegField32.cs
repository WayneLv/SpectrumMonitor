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
    /// A RegField is a BitField within a specific Register.  a RegField will modify a 
    /// a BitField within a register where the bitfield may be in size from 1 bit up to
    /// the whole size of the register.
    /// </summary>
    /// <remarks>
    /// Because registers are involved in literally millions of operations (register read/write)
    /// it is imperative that the implementation is as efficient as possible -- so we exclude
    /// this class from control flow obfuscation
    /// </remarks>
    [System.Reflection.ObfuscationAttribute( Feature = "controlflow=false" )]
    public class RegField32 : BitField
    {
        protected readonly IRegister mRegister;
        protected readonly int mMask;
        private const int REG_SIZE = 32;

        // Create the RegField

        public RegField32( string bitFieldName,
                           int startBit,
                           int sizeInBits,
                           IRegister reg ) : base( bitFieldName, startBit, sizeInBits, REG_SIZE )
        {
            mRegister = reg;
            mMask = (int)CreateMask( REG_SIZE, sizeInBits, startBit );
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
        /// <param name="bitsToSet"></param>
        /// <returns></returns>
        protected virtual int UpdateRegField( int bitsToSet )
        {
            if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
            {
                mLogger.LogAppend(
                    new RegisterLoggingEvent( LogLevel.Fine, bitsToSet, this )
                        {
                            Operation = RegisterLoggingEvent.OperationType.BFSetVal
                        } );
            }

            return ( mRegister.Value32 & ~mMask ) | ( ( bitsToSet << mStartBit ) & mMask );
        }

        protected long ReadBitField()
        {
            uint temp = (uint)mRegister.Value32 & (uint)mMask;
            temp >>= mStartBit;
            long bitFieldVal = temp;
            //long bitFieldVal = (mRegister.Value & mMask) >> mStartBit;


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

        /// <summary>
        /// Gets or Sets the cached latch value, does not access HW.  A set leaves the register
        /// marked dirty. Use Apply() to write dirty values to HW or use Write() to "directly"
        /// write value to HW.
        /// 
        /// Most implementations preserve all bits outside the current bitfield definition,
        /// but some implementations (CommandField32, CommandField64) clear all other bits.
        /// </summary>
        public override int Value
        {
            get
            {
                return (int)ReadBitField();
            }
            set
            {
                // See doc for UpdateRegField -- some versions clear all other bits!
                mRegister.Value32 = UpdateRegField( value );
            }
        }

        /// <summary>
        /// Gets or Sets the cached latch value, does not access HW.  A set leaves the register
        /// marked dirty. Use Apply() to write dirty values to HW or use Write() to "directly"
        /// write value to HW.
        /// 
        /// Most implementations preserve all bits outside the current bitfield definition,
        /// but some implementations (CommandField32, CommandField64) clear all other bits.
        /// </summary>
        public override long Value64
        {
            get
            {
                return ReadBitField();
            }
            set
            {
                // See doc for UpdateRegField -- some versions clear all other bits!
                mRegister.Value32 = UpdateRegField( (int)value );
            }
        }

        /// <summary>
        /// Gets the mask for the bit field within the register.
        /// </summary>
        public override long Mask
        {
            get
            {
                // Casting to (uint) prevents 0x80000000 from turning into 0xffffffff80000000
                return (uint)mMask;
            }
        }

        /// <summary>
        /// Writes the value to HW.
        /// 
        /// Most implementations preserve all bits outside the current bitfield definition,
        /// but some implementations (CommandField32, CommandField64) clear all other bits.
        /// </summary>
        /// <param name="value">value to write.</param>
        public override void Write( int value )
        {
            // See doc for UpdateRegField -- some versions clear all other bits!
            mRegister.Write32( UpdateRegField( value ) );
        }

        /// <summary>
        /// Writes the value to HW.
        /// 
        /// Most implementations preserve all bits outside the current bitfield definition,
        /// but some implementations (CommandField32, CommandField64) clear all other bits.
        /// </summary>
        /// <param name="driver">driver(port in moonstone terms) to write to.</param>
        /// <param name="value">value to write.</param>
        public override void Write( IRegDriver driver, int value )
        {
            // See doc for UpdateRegField -- some versions clear all other bits!
            mRegister.Write32( driver, UpdateRegField( value ) );
        }

        public override int Read()
        {
            mRegister.Read32();
            return (int)ReadBitField();
        }

        /// <summary>
        /// Writes the value to HW.
        /// 
        /// Most implementations preserve all bits outside the current bitfield definition,
        /// but some implementations (CommandField32, CommandField64) clear all other bits.
        /// </summary>
        /// <param name="value">value to write.</param>
        public override void Write( long value )
        {
            // See doc for UpdateRegField -- some versions clear all other bits!
            mRegister.Write32( UpdateRegField( (int)value ) );
        }

        /// <summary>
        /// Reads BitField value from HW.
        /// </summary>
        public override long Read64()
        {
            mRegister.Read32();
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