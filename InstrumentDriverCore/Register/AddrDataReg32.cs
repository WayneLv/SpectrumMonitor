/******************************************************************************
 *                                                                         
 *               
 *               All rights reserved.
 *
 *****************************************************************************/

using System;
using InstrumentDriver.Core.Utility;
using InstrumentDriver.Core.Utility.Log;

namespace InstrumentDriver.Core.Register
{
    /// <summary>
    /// AddrDataReg32 supports the concept of a simple "device registers" that are
    /// accessed via BitFields of address, data, and control (r/w).  The BitFields
    /// are typically part of memory mapped registers but could be implemented by
    /// any type of register
    /// 
    /// For example, with the memory mapped register memReg with BitFields:
    /// 
    ///    memReg:Address ... bits 0..2
    ///    memReg:Data    ... bits 3..19
    ///    memReg:RW      ... bit 20
    /// 
    /// The client code access the device's internal registers by
    /// 
    ///    memReg.Address = 0; memReg.Data=x; memReg.Write=1; memReg.Apply(); // to write reg0=x
    ///    memReg.Address = 1; memReg.Data=y; memReg.Write=1; memReg.Apply(); // to write reg1=y
    ///    memReg.Address = 2; memReg.Data=z; memReg.Write=1; memReg.Apply(); // to write reg2=z
    /// 
    /// To eliminate the need to manipulate the Address field, use this class (AddrDataReg32).
    /// The 'offset' specified in the constructor is the device register address ... and the
    /// calls to read and write will "automatically" set the address and read/write fields.
    /// </summary>
    /// <remarks>
    /// Because registers are involved in literally millions of operations (register read/write)
    /// it is imperative that the implementation is as efficient as possible -- so we exclude
    /// this class from control flow obfuscation
    /// </remarks>
    [System.Reflection.ObfuscationAttribute( Feature = "controlflow=false" )]
    public class AddrDataReg32 : Reg <Int32>
    {
        #region member variables

        protected readonly IBitField mAddrField;
        protected readonly IBitField mDataField;
        protected readonly IBitField mRwField;
        protected readonly int mReadValue;
        protected readonly int mWriteValue;

        #endregion member variables

        #region constructors

        /// <summary>
        /// Construct a "device register" -- the read and write operations are delegated to BitFields
        /// (which could be part of memory mapped registers or other more complicated objects).
        /// Although an IRegDriver is specified, it isn't directly used by AddrDataReg32 but it is
        /// "forwarded" to the BitFields (addrField, dataField, rwField) which may be required for
        /// "control stream operations".
        /// 
        /// If rwField is non-null, it will be set to 1 for writes and 0 for reads
        /// 
        /// *** THIS IMPLEMENTATION ASSUMES ALL IBitField OBJECTS BELONG TO THE SAME REGISTER ***
        /// </summary>
        /// <param name="name">register name</param>
        /// <param name="offset">offset (internal "device address" of the register ... written to 'addrField')</param>
        /// <param name="bfType">enumerated type of BitFields contained by this register.</param>
        /// <param name="driver">the default driver, not directly used by AddrDataReg32 but may be used by the addrField, dataField, rwField</param>
        /// <param name="addrField">BitField for the device's internal register address (offset will be written to this)</param>
        /// <param name="dataField">BitField for the device's internal register data</param>
        /// <param name="rwField">Optional BitField for indicating a read or write operation</param>
        public AddrDataReg32( string name,
                              Int32 offset,
                              Type bfType,
                              IRegDriver driver,
                              IBitField addrField,
                              IBitField dataField,
                              IBitField rwField )
            : this( name, offset, bfType, driver, RegType.RW, addrField, dataField, rwField, 0, 1 )
        {
        }

        /// <summary>
        /// Construct a "device register" -- the read and write operations are delegated to BitFields
        /// (which could be part of memory mapped registers or other more complicated objects).
        /// Although an IRegDriver is specified, it isn't directly used by AddrDataReg32 but it is
        /// "forwarded" to the BitFields (addrField, dataField, rwField) which may be required for
        /// "control stream operations".
        /// 
        /// If rwField is non-null, it will be set to 1 for writes and 0 for reads
        /// 
        /// *** THIS IMPLEMENTATION ASSUMES ALL IBitField OBJECTS BELONG TO THE SAME REGISTER ***
        /// </summary>
        /// <param name="name">register name</param>
        /// <param name="offset">offset (internal "device address" of the register ... written to 'addrField')</param>
        /// <param name="bfType">enumerated type of BitFields contained by this register.</param>
        /// <param name="driver">the default driver, not directly used by AddrDataReg32 but may be used by the addrField, dataField, rwField</param>
        /// <param name="regType">register type/attribute (one or more bits from RegType, e.g. Cmd|RO).</param>
        /// <param name="addrField">BitField for the device's internal register address (offset will be written to this)</param>
        /// <param name="dataField">BitField for the device's internal register data</param>
        /// <param name="rwField">Optional BitField for indicating a read or write operation</param>
        public AddrDataReg32( string name,
                              Int32 offset,
                              Type bfType,
                              IRegDriver driver,
                              RegType regType,
                              IBitField addrField,
                              IBitField dataField,
                              IBitField rwField )
            : this( name, offset, bfType, driver, regType, addrField, dataField, rwField, 0, 1 )
        {
        }

        /// <summary>
        /// Construct a "device register" -- the read and write operations are delegated to BitFields
        /// (which could be part of memory mapped registers or other more complicated objects).
        /// Although an IRegDriver is specified, it isn't directly used by AddrDataReg32 but it is
        /// "forwarded" to the BitFields (addrField, dataField, rwField) which may be required for
        /// "control stream operations".
        /// 
        /// If rwField is non-null, it will be set to writeValue for writes and readValue for reads
        /// 
        /// *** THIS IMPLEMENTATION ASSUMES ALL IBitField OBJECTS BELONG TO THE SAME REGISTER ***
        /// </summary>
        /// <param name="name">register name</param>
        /// <param name="offset">offset (internal "device address" of the register ... written to 'addrField')</param>
        /// <param name="bfType">enumerated type of BitFields contained by this register.</param>
        /// <param name="driver">the default driver, not directly used by AddrDataReg32 but may be used by the addrField, dataField, rwField</param>
        /// <param name="regType">register type/attribute (one or more bits from RegType, e.g. Cmd|RO).</param>
        /// <param name="addrField">BitField for the device's internal register address (offset will be written to this)</param>
        /// <param name="dataField">BitField for the device's internal register data</param>
        /// <param name="rwField">Optional BitField for indicating a read or write operation</param>
        /// <param name="readValue">The value written to rwField for read operations</param>
        /// <param name="writeValue">The value written to rwField for write operations</param>
        public AddrDataReg32( string name,
                              Int32 offset,
                              Type bfType,
                              IRegDriver driver,
                              RegType regType,
                              IBitField addrField,
                              IBitField dataField,
                              IBitField rwField,
                              int readValue,
                              int writeValue )
            : base( name, offset, bfType, driver, regType )
        {
            mAddrField = addrField;
            mDataField = dataField;
            mRwField = rwField;
            mReadValue = readValue;
            mWriteValue = writeValue;
        }

        #endregion constructors

        #region Implementation of RegBase/Reg abstract methods

        /// <summary>
        /// Perform a read of the register WITHOUT updating the register's software
        /// copy or performing any of the normal checks (such as "is this register
        /// readable?").  This method is intended for advanced operations such as
        /// used by RegSet -- please consider using Read() instead.
        /// 
        /// For AddrDataReg32 this is a "device register" read operation.  This is composed of
        /// 1) Optionally, a write mReadValue to mRwField (if non-null)
        /// 2) A write to mAddrField (value == Offset)
        /// 3) A read from mDataField
        /// </summary>
        /// <returns></returns>
        public override int DriverRead()
        {
            // Make sure control and address fields are set...
            if( mRwField != null )
            {
                mRwField.Value = mReadValue;
            }
            mAddrField.Value = Offset;

            // *** THIS IMPLEMENTATION ASSUMES ALL IBitField OBJECTS BELONG TO THE SAME REGISTER ***
            mAddrField.Apply( false );

            // Delegate actual read to the Field (instead of the normal IRegDriver)
            int regVal = mDataField.Read();

            if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
            {
                mLogger.LogAppend( new RegisterLoggingEvent( LogLevel.Fine, regVal, this )
                    { Operation = RegisterLoggingEvent.OperationType.RegRdDR } );
            }
            return regVal;
        }

        /// <summary>
        /// Perform a write of the register using the register's software copy of its
        /// value without performing any of the normal checks (such as "is this register
        /// writable?").  This method is intended for advanced operations such as
        /// used by RegSet -- please consider using Write( IRegDriver, T ) instead.
        /// 
        /// For AddrDataReg32 this is a "device register" write operation.  This is composed of
        /// 1) Optionally, a write mWriteValue to mRwField (if non-null)
        /// 2) A write to mAddrField (value == Offset)
        /// 3) A write to mAddrField
        /// If all the fields are part of the same register, all the fields are written at the same time.
        /// </summary>
        /// <param name="driver">IRegDriver used for memory mapped register operation (forwarded to BitField.Apply)</param>
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

            // Make sure control and address fields are set...
            if( mRwField != null )
            {
                mRwField.Value = mWriteValue;
            }
            mAddrField.Value = Offset;
            mDataField.Value = mValue;
            // *** THIS IMPLEMENTATION ASSUMES ALL IBitField OBJECTS BELONG TO THE SAME REGISTER ***
            // NOTE: DriverWrite() always writes to the HW ... so set Force=true
            mDataField.Apply( driver, true );
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
        /// <param name="regDef">reference to RegDeg struct.</param>
        /// <param name="driver">driver to read this type of Treg register.</param>
        /// <param name="baseAddr">typically 0, but this additional base address can be
        /// used to add to the register address specified in the RegDef struct. 
        /// Currently only used for CannotReadDirectly registers, so is typically 0.</param>
        /// <param name="args">arbitrary array of objects for use by the delegate ... For
        /// AddrDataReg32 this must contain (in order):
        ///    Address Field (RegField32)
        ///    Data Field (RegField32)
        ///    Control Field (RegField32)
        /// </param>
        /// <returns>a reference to the register Treg created.</returns>
        public static IRegister ConstructReg( string name,
                                              RegDef regDef,
                                              IRegDriver driver,
                                              int baseAddr,
                                              object[] args )
        {
            if( args == null ||
                args.Length < 3 )
            {
                throw new InvalidParameterException(
                    "Expected 'args' to contain 3 RegField32 values [address, data, control]." );
            }
            switch( args.Length )
            {
                case 3:
                    return new AddrDataReg32( name,
                                              regDef.BARoffset + baseAddr,
                                              regDef.BFenum,
                                              driver,
                                              regDef.RegType,
                                              (IBitField)args[ 0 ],
                                              (IBitField)args[ 1 ],
                                              (IBitField)args[ 2 ] );
                case 5:
                    return new AddrDataReg32( name,
                                              regDef.BARoffset + baseAddr,
                                              regDef.BFenum,
                                              driver,
                                              regDef.RegType,
                                              (IBitField)args[ 0 ],
                                              (IBitField)args[ 1 ],
                                              (IBitField)args[ 2 ],
                                              (int)args[ 3 ],
                                              (int)args[ 4 ] );
                default:
                    throw new InvalidParameterException(
                        "Expected 'args' to contain 3 or 5 values: 3 RegField32 values [address, data, control] and, optionally, read control value and write control value." );
            }
        }

        /// <summary>
        /// Register 'AddrDataReg32' with RegFactory so AddrDataReg32 can be instantiated via a text lookup
        /// (typically by RegFactory.CreateRegArray( Stream, IRegManager )).
        /// 
        /// See RegManager.RegisterType for more details.
        /// </summary>
        public static void RegisterTypeWithFactory()
        {
            RegFactory.RegisterType( "AddrDataReg32", typeof( AddrDataReg32 ) );
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