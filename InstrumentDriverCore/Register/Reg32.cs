/******************************************************************************
 *
 *               
 *               All rights reserved.
 *
 *****************************************************************************/

using System;
using System.Reflection;
using InstrumentDriver.Core.Utility.Log;

namespace InstrumentDriver.Core.Register
{
    /// <summary>
    /// Register implementation
    /// </summary>
    /// <remarks>
    /// Because registers are involved in literally millions of operations (register read/write)
    /// it is imperative that the implementation is as efficient as possible -- so we exclude
    /// this class from control flow obfuscation
    /// </remarks>
    [System.Reflection.ObfuscationAttribute( Feature = "controlflow=false" )]
    public class Reg32 : Reg <Int32>
    {
        #region constructors

        public Reg32( string name, Int32 offset, Type bfType, IRegDriver driver )
            : this( name, offset, bfType, driver, RegType.RW )
        {
        }

        public Reg32( string name,
                      Int32 offset,
                      Type bfType,
                      IRegDriver driver,
                      RegType eRegType )
            : base( name, offset, bfType, driver, eRegType )
        {
            // Typically (not always), Reg32 is memory mapped
            IsMemoryMapped = true;
        }

        #endregion constructors

        #region Implementation of RegBase/Reg abstract methods

        /// <summary>
        /// Perform a read of the register WITHOUT updating the register's software
        /// copy or performing any of the normal checks (such as "is this register
        /// readable?").  This method is intended for advanced operations such as
        /// used by RegSet -- please consider using Read() instead.
        /// </summary>
        /// <returns></returns>
        public override int DriverRead()
        {
            int regVal = mDriver.RegRead( mOffset );

            //if( mLogFunctionLogElement != null )
            //{
            //    mLogFunctionLogElement( this, regVal, ItemLogged.RegRd );
            //}

            if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
            {
                mLogger.LogAppend( new RegisterLoggingEvent( LogLevel.Fine, regVal, this )
                    { Operation = RegisterLoggingEvent.OperationType.RegRd } );
            }

            return regVal;
        }

        /// <summary>
        /// Perform a write of the register using the register's software copy of its
        /// value without performing any of the normal checks (such as "is this register
        /// writable?").  This method is intended for advanced operations such as
        /// used by RegSet -- please consider using Write( IRegDriver, T ) instead.
        /// </summary>
        /// <returns></returns>
        public override void DriverWrite( IRegDriver driver )
        {
            // REVISIT: should put lock around these otherwise in a multi threaded environment
            // you can't be sure the value logged was actually the value written!!!
            // ACTUALLY, we could just get a thread safe, stack copy of mValue at start of this 
            // method and use it internally.  This would fix 1 problem.  It wouldn't however fix
            // the problem of really not being sure the write took place when it was supposed to!
            //if( mLogFunctionLogElement != null )
            //{
            //    mLogFunctionLogElement( this,
            //                            mValue,
            //                            ( driver == null || ReferenceEquals( driver, mDriver ) )
            //                                ? ItemLogged.RegWr
            //                                : ItemLogged.RegWrCS );
            //}

            if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
            {
                mLogger.LogAppend( new RegisterLoggingEvent( LogLevel.Fine, mValue, this )
                    {
                        Operation = ( driver == null || driver.IsRecordingSession == false )
                                        ? RegisterLoggingEvent.OperationType.RegWr
                                        : RegisterLoggingEvent.OperationType.RegWrCS
                    } );
            }
            if( driver == null )
            {
                mDriver.RegWrite( mOffset, mValue );
            }
            else
            {
                // Explicitly specify AddressSpace -- the register may be associated
                // with a different space than the specified driver...
                driver.RegWrite( mDriver.AddressSpace, mOffset, mValue );
            }
        }


        public override int SizeInBytes
        {
            get
            {
                return sizeof( Int32 );
            }
        }

        #region Value/Read/Write pseudo-polymorphism

        /// <summary>
        /// Set or get the software copy of the registers value.
        /// No hardware access will take place.
        /// If this is called on a 64 bit register, the implementation may
        /// either throw an exception or
        ///   get: truncate the value to 32 bits.
        ///   set: zero pad the value to 64 bits.
        /// </summary>
        public override int Value32
        {
            get
            {
                return Value;
            }
            set
            {
                Value = value;
            }
        }

        /// <summary>
        /// Set or get the software copy of the registers value.
        /// No hardware access will take place.
        /// If this is called on a 32 bit register, the implementation may
        /// either throw an exception or
        ///   get: zero pad the value to 64 bits.
        ///   set: truncate the value to 32 bits.
        /// </summary>
        public override long Value64
        {
            get
            {
                return (uint)Value;
            }
            set
            {
                Value = (int)value;
            }
        }

        /// <summary>
        /// Reads a Register value from HW. 
        /// The software copy of the registers value is also updated.
        /// If this is called on a 64 bit register, the implementation may
        /// either throw an exception or truncate the value to 32 bits.
        /// </summary>
        /// <returns></returns>
        public override int Read32()
        {
            return Read();
        }

        /// <summary>
        /// Reads a Register value from HW. 
        /// The software copy of the registers value is also updated.
        /// If this is called on a 32 bit register, the implementation may
        /// either throw an exception or zero pad the value to 64 bits.
        /// </summary>
        /// <returns></returns>
        public override long Read64()
        {
            return (uint)Read();
        }

        /// <summary>
        /// Writes a register value to HW.  The software copy of the registers value is updated.
        /// If this is called on a 64 bit register, the implementation may
        /// either throw an exception or zero pad the value to 64 bits.
        /// </summary>
        /// <param name="registerValue">the value to write to HW></param>
        public override void Write32( int registerValue )
        {
            Write( registerValue );
        }

        /// <summary>
        /// Writes a register value to HW using the specified IRegDriver.
        /// The software copy of the registers value is also updated.
        /// If this is called on a 64 bit register, the implementation may
        /// either throw an exception or zero pad the value to 64 bits.
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="registerValue">the value to write to HW></param>
        public override void Write32( IRegDriver driver, int registerValue )
        {
            Write( driver, registerValue );
        }

        /// <summary>
        /// Writes a register value to HW.  The software copy of the registers value is updated.
        /// If this is called on a 32 bit register, the implementation may
        /// either throw an exception or truncate the value to 32 bits.
        /// </summary>
        /// <param name="registerValue">the value to write to HW></param>
        public override void Write64( long registerValue )
        {
            Write( (int)registerValue );
        }

        /// <summary>
        /// Writes a register value to HW using the specified IRegDriver.
        /// The software copy of the registers value is also updated.
        /// If this is called on a 32 bit register, the implementation may
        /// either throw an exception or truncate the value to 32 bits.
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="registerValue">the value to write to HW></param>
        public override void Write64( IRegDriver driver, long registerValue )
        {
            Write( driver, (int)registerValue );
        }

        /// <summary>
        /// Synchronize the cached register value to the supplied value.
        /// If there is a pending/dirty value (ie. Apply hasn't been called) the
        /// pending value is lost and the dirty flag is cleared.
        /// 
        /// A typical use of this method is if a block/buffer read of memory has been
        /// performed (for speed) and the registers corresponding to the block need
        /// to be updated to match the read.
        /// </summary>
        /// <param name="value"></param>
        public override void UpdateRegVal32( int value )
        {
            mValue = value;
            NeedApply = false;
        }

        /// <summary>
        /// Synchronize the cached register value to the supplied value.
        /// If there is a pending/dirty value (ie. Apply hasn't been called) the
        /// pending value is lost and the dirty flag is cleared.
        /// 
        /// A typical use of this method is if a block/buffer read of memory has been
        /// performed (for speed) and the registers corresponding to the block need
        /// to be updated to match the read.
        /// </summary>
        /// <param name="value"></param>
        public override void UpdateRegVal64( long value )
        {
            mValue = (int)value;
            NeedApply = false;
        }

        #endregion Value/Read/Write pseudo-polymorphism

        #endregion Implementation of RegBase/Reg abstract methods

        #region Delegate/Factory methods

        /// <summary>
        /// Delegate method used to construct a register.  This method will be passed to a
        /// register factory such as RegFactory to create registers from definitions.
        /// </summary>
        /// <param name="name">register name</param>
        /// <param name="regDef">reference to RegDef struct.</param>
        /// <param name="driver">driver to read this type of Treg register.</param>
        /// <param name="baseAddr">typically 0, but this additional base address can be
        /// used to add to the register address specified in the RegDef struct. 
        /// Currently only used for CannotReadDirectly registers, so is typically 0.</param>
        /// <param name="args">arbitrary array of objects for use by the delegate ... normally
        /// used to specify register-type specific arguments</param>
        /// <returns>a reference to the register Treg created.</returns>
        /// <remarks>
        /// Runtime register creation assumes ONE of the following is true
        /// 1) ConstructReg is not renamed by obfuscation (so it can be found by name using reflection)
        /// 2) The signature is unique for the class (so it can be found by signature using reflection)
        /// Since Reg32 includes multiple factory methods with the same signature it must be excluded from obfuscation
        /// </remarks>
        [ObfuscationAttribute( Exclude = true )]
        public static IRegister ConstructReg( string name,
                                              RegDef regDef,
                                              IRegDriver driver,
                                              int baseAddr,
                                              object[] args )
        {
            return new Reg32( name,
                              regDef.BARoffset + baseAddr,
                              regDef.BFenum,
                              driver,
                              regDef.RegType );
        }

        /// <summary>
        /// Delegate method used to construct a register.  This method will be passed to a
        /// register factory such as RegFactory to create registers from definitions.
        /// </summary>
        /// <param name="name">register name</param>
        /// <param name="regDef">reference to RegDef struct.</param>
        /// <param name="driver">driver to read this type of Treg register.</param>
        /// <param name="baseAddr">typically 0, but this additional base address can be
        /// used to add to the register address specified in the RegDef struct. 
        /// Currently only used for CannotReadDirectly registers, so is typically 0.</param>
        /// <param name="args">arbitrary array of objects for use by the delegate ... normally
        /// used to specify register-type specific arguments</param>
        /// <returns>a reference to the register Treg created.</returns>
        public static IRegister ConstructNonMemoryMappedReg( string name,
                                                             RegDef regDef,
                                                             IRegDriver driver,
                                                             int baseAddr,
                                                             object[] args )
        {
            return new Reg32( name,
                              regDef.BARoffset + baseAddr,
                              regDef.BFenum,
                              driver,
                              regDef.RegType )
                {
                    IsMemoryMapped = false
                };
        }

        /// <summary>
        /// Register 'Reg32' with RegFactory so Reg32 can be instantiated via a text lookup
        /// (typically by RegFactory.CreateRegArray( Stream, IRegManager )).
        /// 
        /// See RegManager.RegisterType for more details.
        /// </summary>
        public static void RegisterTypeWithFactory()
        {
            RegFactory.RegisterType( "Reg32", typeof( Reg32 ) );
        }

        #endregion Delegate/Factory methods

        #region Register bit locking methods

        /// <summary>
        /// Prevent register bits from being modified.
        /// </summary>
        /// <param name="lockMask">The desired mask of locked bits></param>
        public override void LockBits32(Int32 lockMask)
        {
            LockBits(lockMask);
        }

        /// <summary>
        /// Prevent register bits from being modified.
        /// </summary>
        /// <param name="lockMask">The desired mask of locked bits></param>
        public override void LockBits64(Int64 lockMask)
        {
            LockBits((Int32)lockMask);
        }

        /// <inheritdoc />
        public override Int32 GetLockMask32()
        {
            return GetLockMask();
        }

        /// <inheritdoc />
        public override Int64 GetLockMask64()
        {
            return (Int64)GetLockMask();
        }

        /// <inheritdoc />
        protected override Int32 createLockMask(int startBit, int bitCount)
        {
            if ((startBit == 0) && (bitCount == 0))
            {
                // Create a mask for all bits in the register.
                return ~0;
            }

            return (((1 << bitCount) - 1) << startBit);
        }

        /// <inheritdoc />
        protected override void updateLockMask(ref Int32 currentMask, Int32 newMask, bool addMask)
        {
            if (addMask)
            {
                // Add the specified bits to the mask.
                currentMask |= newMask;
            }
            else
            {
                // Remove the specified bits from the mask.
                currentMask &= ~newMask;
            }
        }

        /// <inheritdoc />
        protected override void applyLockMask(ref Int32 currentValue, Int32 newValue, Int32 lockMask)
        {
            if (lockMask == 0)
            {
                // Optimized case when no bits are locked.
                currentValue = newValue;

                return;
            }

            // Update the current register value, preserving locked bits.
            currentValue = ((currentValue & lockMask) | (newValue & ~lockMask));
        }

        #endregion Register bit locking methods
    }
}