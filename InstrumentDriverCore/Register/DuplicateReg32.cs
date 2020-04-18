using System;
using InstrumentDriver.Core.Utility;
using InstrumentDriver.Core.Utility.Log;

namespace InstrumentDriver.Core.Register
{
    /// <summary>
    /// This is a useful register when you want to treat two registers as one register.  For example
    /// for a multi-slug module there code be registers that have map on to the slugs to configure
    /// the ABUS so that nodes are configured on the multiplexer and that the "unit select" is
    /// set such that routing across the boards is set up properly.
    /// 
    /// Note: Caveat-For this class to work, the "duplicate" registers must have common (duplicate) bitfield definitions.
    /// All this class does is to make sure the same value is Written to the list of registers.  On
    /// Reading it is assumed after the first write that the registers will mirror each other.
    /// 
    /// It is OK if the registers differ in defines except for the fact that the bitfields defined for the
    /// "Duplicate Register" are common across the registers.
    /// 
    /// For example
    /// 
    /// DuplicateReg32
    ///    Field 1 - Bits 30:31  
    ///    Field 2 - Bits 24:29
    /// 
    /// Register[0]
    ///    Field 1 - Bits 30:31  
    ///    Field 2 - Bits 24:29
    ///    Field 3 - Bits 0:23
    /// 
    /// Register[1]
    ///    Field 1 - Bits 30:31  
    ///    Field 2 - Bits 24:29
    ///              Bits 0:23 are unused
    /// 
    /// 
    /// </summary>
    [System.Reflection.ObfuscationAttribute( Feature = "controlflow=false" )]
    public class DuplicateReg32 : Reg <Int32>
    {
        private readonly IRegister[] mRegisters;

        /// <summary>
        /// We will lazily infer the "common mask" which duplicates the fields
        /// across the registers.  We can't do this at construction because
        /// the bitfields are defined after the register is created. 
        /// </summary>
        private uint mCommonMask;

        /// <summary>
        /// Construct a register with RW (read-write) attribute.
        /// </summary>
        /// <param name="name">register name</param>
        /// <param name="bfType">enumerated type of BitFields contained by this register.</param>
        /// <param name="driver">The default driver the register uses to read/write.</param>
        /// <param name="registers">The list of registers to duplicate values over</param>
        public DuplicateReg32( string name, Type bfType, IRegDriver driver, IRegister[] registers )
            : this( name, bfType, driver, RegType.RW, registers )
        {
        }

        /// <summary>
        /// Construct a register with the specified register attributes
        /// </summary>
        /// <param name="name">register name</param>
        /// <param name="bfType">enumerated type of BitFields contained by this register.</param>
        /// <param name="regType">register type/attribute (one or more bits from RegType, e.g. Cmd|RO).</param>
        /// <param name="driver">The default driver the register uses to read/write.</param>
        /// <param name="registers">The list of registers to duplicate values over</param>
        public DuplicateReg32( string name,
                               Type bfType,
                               IRegDriver driver,
                               RegType regType,
                               IRegister[] registers )
            : base( name, 0, bfType, driver, regType ) // 0 offset
        {
            mRegisters = registers;

            // ReSharper disable DoNotCallOverridableMethodsInConstructor
            if( Fields.Length == 0 )
            {
                throw new InternalApplicationException( "A DuplicateReg32 definition must have at least one field." );
            }
            // ReSharper restore DoNotCallOverridableMethodsInConstructor
        }

        #region Overrides of RegBase

        /// <summary>
        /// The size, in bytes, of the register data.
        /// </summary>
        public override int SizeInBytes
        {
            get
            {
                return sizeof( Int32 );
            }
        }

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

        #endregion

        #region Overrides of Reg<int>

        /// <summary>
        /// Perform a read of the register WITHOUT updating the register's software
        /// copy or performing any of the normal checks (such as "is this register
        /// readable?").  This method is intended for advanced operations such as
        /// used by RegSet -- please consider using Read() instead.
        /// </summary>
        /// <returns></returns>
        public override int DriverRead()
        {
            // Delegate actual read to the Field (instead of the normal IRegDriver)
            // This should mirror each other after the first write, just return
            // the last value...
            int regVal = 0;
            foreach( var register in mRegisters )
            {
                regVal = register.Read32();
            }

            if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
            {
                mLogger.LogAppend( new RegisterLoggingEvent( LogLevel.Fine, regVal, this )
                    {
                        Operation = RegisterLoggingEvent.OperationType.RegRdDR
                    } );
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

            if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
            {
                mLogger.LogAppend( new RegisterLoggingEvent( LogLevel.Fine, mValue, this )
                    {
                        Operation = RegisterLoggingEvent.OperationType.RegWrDR
                    } );
            }

            // Lazily initialize the common mask...
            if( mCommonMask == 0 )
            {
                //  We need to create the common mask from the defined fields...
                foreach( var field in Fields )
                {
                    mCommonMask |= (uint)field.Mask;
                }
            }

            uint invMask = ~mCommonMask;

            // All the registers should get written with exactly the same value...
            foreach( var register in mRegisters )
            {
                // Need to remove the existing bits, then
                // 'OR' in the register value in case the registers use other bit fields
                // for something else.
                register.Value32 = (int)( (uint)( register.Value32 & invMask ) | (uint)mValue );
                // NOTE: DriverWrite() always writes to the HW ... so set Force=true
                register.Apply( driver, true );
            }
        }

        #endregion

        /// <summary>
        /// Register 'DuplicateReg32' with RegFactory so DuplicateReg32 can be instantiated via a text lookup
        /// (typically by RegFactory.CreateRegArray( Stream, IRegManager )).
        /// 
        /// See RegManager.RegisterType for more details.
        /// </summary>
        public static void RegisterTypeWithFactory()
        {
            RegFactory.RegisterType( "DuplicateReg32", typeof( DuplicateReg32 ) );
        }

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