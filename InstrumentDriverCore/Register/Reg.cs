/******************************************************************************
 *                                                                         
 *               
 *               All rights reserved.
 *
 *****************************************************************************/

using System;
using InstrumentDriver.Core.Utility.Log;

namespace InstrumentDriver.Core.Register
{
    /// <summary>
    /// Register class that allows BitFields of type TRegField to exist within a Register.
    /// </summary>
    /// <remarks>
    /// T = Int32 or Int64
    /// 
    /// Because registers are involved in literally millions of operations (register read/write)
    /// it is imperative that the implementation is as efficient as possible -- so we exclude
    /// this class from control flow obfuscation
    /// </remarks>
    [System.Reflection.ObfuscationAttribute( Feature = "controlflow=false" )]
    public abstract class Reg < T > : RegBase
        where T : struct
    {
        #region member variables

        protected T mValue;
        private T mPushedValue;
        private bool mPushedDirty;
        private T mLockMask;

        #endregion member variables

        #region constructors

        /// <summary>
        /// Construct a register with RW (read-write) attribute.
        /// </summary>
        /// <param name="name">register name</param>
        /// <param name="offset">offset (BAR offset for memory mapped register)</param>
        /// <param name="bfType">enumerated type of BitFields contained by this register.</param>
        /// <param name="driver">The default driver the register uses to read/write.</param>
        protected Reg( string name, Int32 offset, Type bfType, IRegDriver driver )
            : this( name, offset, bfType, driver, RegType.RW )
        {
        }

        /// <summary>
        /// Construct a register with the specified register attributes
        /// </summary>
        /// <param name="name">register name</param>
        /// <param name="offset">offset (BAR offset for memory mapped register)</param>
        /// <param name="bfType">enumerated type of BitFields contained by this register.</param>
        /// <param name="regType">register type/attribute (one or more bits from RegType, e.g. Cmd|RO).</param>
        /// <param name="driver">The default driver the register uses to read/write.</param>
        protected Reg( string name,
                       Int32 offset,
                       Type bfType,
                       IRegDriver driver,
                       RegType regType )
            : base( name, offset, bfType, regType, driver )
        {
            if( bfType != null )
            {
                // mFields = new TRegField[Enum.GetNames(BFenumType).Length];

                // PREPARING FOR UNUSED COMMON BITS
                Array values = Enum.GetValues( bfType );
                mFirstBFvalue = (byte)( (int)values.GetValue( 0 ) );
                mNumBFs = (byte)values.Length;

                //int maxValue = 0;
                //foreach (int value in values)
                //{
                //   if (value > maxValue)
                //      maxValue = value;
                //   if (value < mFirstField)
                //      mFirstField = (byte)value;
                //}
                // we create an array for enum values 0-LastBFvalue
                mFields = new IBitField[LastBF + 1];
                // mNumBFs = (byte) (maxValue+1);
            }
            else
            {
                mFields = null;
            }
            Reset(); // reset IEnumerable index.
        }

        #endregion constructors

        /// <summary>
        /// Set or get the software copy of the registers value. No hardware access will take place.
        /// </summary>
        public T Value
        {
            get
            {
                if( IsVolatileReadWrite )
                {
                    // Normally we want to (re-)read the current content of volatile registers - however
                    // if the client code has made changes to the register then reading the content would
                    // discard the modifications, so only read if there are no pending changes (use the
                    // mDirty flag instead of NeedApply because NeedApply may return the value of the
                    // group dirty bit).
                    if( ! mDirty )
                    {
                        Read();
                    }
                }
                return mValue;
            }
            set
            {
                // mValue and the corresponding write to hardware must be thread safe
                lock( mResource )
                {
                    if( IsReadOnly || IsNoValue )
                    {
                        mLogger.LogAppend( new LoggingEvent( LogLevel.Warning,
                                                             string.Format(
                                                                 "Attempt to set value of RO or NoValue register ({0} / 0x{1:x}",
                                                                 Name,
                                                                 Offset ),
                                                             this ) );
                        return;
                    }

                    // NOTE: the original APeX code did not force writes for Cmd/Event registers (not sure
                    //       why unless this activity always went through Fields?)
                    if( !value.Equals( mValue ) || IsCommand || IsEvent || IsNoValueFilter )
                    {
                        mValue = value;
                        NeedApply = true;
                    }
                }
            }
        }

        /// <summary>
        /// Reads a Register value from HW. The software copy of the registers value is updated.
        /// </summary>
        /// <returns></returns>
        public T Read()
        {
            // can't read CannotReadDirectly regs directly from hw.
            if( ! IsWriteOnly && ! IsCannotReadDirectly )
            {
                UpdateRegVal();
            }
            return mValue;
        }

        // these abstract methods must be defined in a derived class.

        /// <summary>
        /// Perform a read of the register WITHOUT updating the register's software
        /// copy or performing any of the normal checks (such as "is this register
        /// readable?").  This method is intended for advanced operations such as
        /// used by RegSet -- please consider using Read() instead.
        /// </summary>
        /// <returns></returns>
        public abstract T DriverRead();

        /// <summary>
        /// Perform a write of the register using the register's software copy of its
        /// value without performing any of the normal checks (such as "is this register
        /// writable?").  This method is intended for advanced operations such as
        /// used by RegSet -- please consider using Write( IRegDriver, T ) instead.
        /// </summary>
        /// <returns></returns>
        public abstract void DriverWrite( IRegDriver driver );

        /// <summary>
        /// Perform a write of the register using the register's software copy of its
        /// value without performing any of the normal checks (such as "is this register
        /// writable?").  This method is intended for advanced operations such as
        /// used by RegSet -- please consider using Write( T ) instead.
        /// </summary>
        /// <returns></returns>
        public virtual void DriverWrite()
        {
            DriverWrite( mDriver );
        }


        // update reg value from hardware
        public override void UpdateRegVal()
        {
            // NOTE: this is called from the RegBase constructor -- only access RegBase objects!!!
            if( ! IsWriteOnly )
            {
                // mValue and the corresponding write to hardware must be thread safe
                lock( mResource )
                {
                    NeedApply = false;
                    mValue = DriverRead();
                }
            }
        }

        /// <summary>
        /// Writes a register value to HW.  The software copy of the registers value is updated.
        /// </summary>
        /// <param name="registerValue">the value to write to HW></param>
        public void Write( T registerValue )
        {
            //           if (!m_locked)
            //if (!m_bInApply)  // do not allow writes while apply is active.
            if( IsReadOnly )
            {
                if( mLogger.IsLoggingEnabledFor( LogLevel.Warning ) )
                {
                    mLogger.LogAppend( new LoggingEvent( LogLevel.Warning,
                                                         string.Format(
                                                             "Attempt to write to RO register ({0} / 0x{1:x}",
                                                             Name,
                                                             Offset ),
                                                         this ) );
                }
                return;
            }

            // mValue and the corresponding write to hardware must be thread safe
            lock( mResource )
            {
                mValue = registerValue;

                if( mRegSet != null )
                {
                    mRegSet.DriverWrite( mDriver );
                }
                else
                {
                    DriverWrite( mDriver );
                }

                NeedApply = false;
            }
        }

        /// <summary>
        /// Writes a register value to HW using the specified IRegDriver.
        /// The software copy of the registers value is updated.
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="registerValue">the value to write to HW></param>
        public void Write( IRegDriver driver, T registerValue )
        {
            if( IsReadOnly )
            {
                if( mLogger.IsLoggingEnabledFor( LogLevel.Warning ) )
                {
                    mLogger.LogAppend( new LoggingEvent( LogLevel.Warning,
                                                         string.Format(
                                                             "Attempt to write to RO register ({0} / 0x{1:x}",
                                                             Name,
                                                             Offset ),
                                                         this ) );
                }
                return;
            }

            // mValue and the corresponding write to hardware must be thread safe
            lock( mResource )
            {
                mValue = registerValue;

                if( mRegSet != null )
                {
                    mRegSet.DriverWrite( driver );
                }
                else
                {
                    DriverWrite( driver );
                }

                NeedApply = false;
            }
        }

        public override void Apply( IRegDriver driver, bool forceApply )
        {
            if( IsApplyEnabled && ( NeedApply || ( forceApply && IsForceAllowed ) ) )
            {
                // This check (for RO and NoValue) filters out a very small percentage of write requests
                // but takes almost as long as the 'if statement' immediately preceding.  The preceding
                // check typically filters out >90% of the Apply calls ... so it makes more sense (performance
                // wise) to have the RO | NoValue check here rather than as the first statement of Apply()
                if( IsReadOnly || IsNoValue )
                {
                    // Don't generate a warning here -- calling Apply on any register is legal/OK
                    return;
                }

                if( mRegSet != null )
                {
                    mRegSet.DriverWrite( driver );
                }
                else
                {
                    DriverWrite( driver );
                }

                NeedApply = false;
            }
        }

        public override void Apply( bool forceApply )
        {
            Apply( mDriver, forceApply );
        }

        /// <summary>
        /// "Push" the current cached register state (normally the cached register value and the dirty
        /// flag).  Default implementation is a "stack depth" of 1 (i.e. multiple pushes have no effect).
        /// 
        /// This is intended to work in conjunction with "recording" implementations of IRegDriver (e.g.
        /// IRecordingControl) -- prior to recording an operation the client calls "Push", enables recording,
        /// then performs various register operations (which normally do NOT update the hardware), then
        /// calls "Pop" so the cached register state matches the hardware...
        /// 
        /// This does NOT address module/object properties (normally implementations of ICoreSettings) --
        /// the recording process typically changes property values and calling Apply ultimately clears
        /// any pending change flags.
        /// </summary>
        public override void Push()
        {
            lock( mResource )
            {
                // Push value & dirty onto the "stack" (1 deep)
                mPushedValue = mValue;
                mPushedDirty = mDirty;
            }
        }

        /// <summary>
        /// "Pop" the saved register state (normally the cached register value and the dirty flag).
        /// Default implementation is a "stack depth" of 1 (i.e. multiple pops have no effect).
        /// 
        /// This is intended to work in conjunction with "recording" implementations of IRegDriver (e.g.
        /// IRecordingControl) -- prior to recording an operation the client calls "Push", enables recording,
        /// then performs various register operations (which normally do NOT update the hardware), then
        /// calls "Pop" so the cached register state matches the hardware...
        /// 
        /// This does NOT address module/object properties (normally implementations of ICoreSettings) --
        /// the recording process typically changes property values and calling Apply ultimately clears
        /// any pending change flags.
        /// </summary>
        public override void Pop()
        {
            lock( mResource )
            {
                // Pop value & dirty from the "stack" (1 deep)
                mValue = mPushedValue;
                mDirty = mPushedDirty;
            }
        }

        #region Register bit locking methods

        /// <summary>
        /// Prevent register bits from being modified.
        /// </summary>
        /// <param name="lockMask">The desired mask of locked bits></param>
        public void LockBits(T lockMask)
        {
            lock (mResource)
            {
                mLockMask = lockMask;
            }
        }

        /// <summary>
        /// Prevent register bits from being modified.
        /// </summary>
        /// <param name="startBit">Starting bit to lock></param>
        /// <param name="bitCount">Number of bits to lock></param>
        public override void LockBits(int startBit, int bitCount)
        {
            lock (mResource)
            {
                updateLockMask(ref mLockMask, createLockMask(startBit, bitCount), true);
            }
        }

        /// <summary>
        /// Prevent a register from being modified.
        /// </summary>
        public override void LockBits()
        {
            LockBits(0, 0);
        }

        /// <summary>
        /// Allow register bits to be modified.
        /// </summary>
        /// <param name="startBit">Starting bit to unlock></param>
        /// <param name="bitCount">Number of bits to unlock></param>
        public override void UnlockBits(int startBit, int bitCount)
        {
            lock (mResource)
            {
                updateLockMask(ref mLockMask, createLockMask(startBit, bitCount), false);
            }
        }

        /// <summary>
        /// Allow a register to be modified.
        /// </summary>
        public override void UnlockBits()
        {
            UnlockBits(0, 0);
        }

        /// <summary>
        /// Query a register's lock mask.
        /// </summary>
        /// <returns></returns>
        public T GetLockMask()
        {
            return mLockMask;
        }

        /// <summary>
        /// Create a mask for register bit locking.
        /// </summary>
        /// <param name="startBit">Starting bit to lock></param>
        /// <param name="bitCount">Number of bits to lock></param>
        /// <returns></returns>
        protected abstract T createLockMask(int startBit, int bitCount);

        /// <summary>
        /// Update the register bit lock mask
        /// </summary>
        /// <param name="currentMask">Current lock mask></param>
        /// <param name="newMask">Mask bits to add or delete></param>
        /// <param name="addMask">true if adding bits to mask></param>
        protected abstract void updateLockMask(ref T currentMask, T newMask, bool addMask);

        /// <summary>
        /// Update register value preserving locked bits
        /// </summary>
        /// <param name="currentValue">Current register value></param>
        /// <param name="newValue">New register value</param>
        /// <param name="lockMask">Mask of locked bits</param>
        protected abstract void applyLockMask(ref T currentValue, T newValue, T lockMask);

        #endregion Register bit locking methods
    }
}