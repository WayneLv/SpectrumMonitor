/******************************************************************************
 *                                                                         
 *               
 *               All rights reserved.
 *
 *****************************************************************************/

using System;
using System.Text;
using InstrumentDriver.Core.Common.IO;
using InstrumentDriver.Core.Utility;
using InstrumentDriver.Core.Utility.Log;

namespace InstrumentDriver.Core.Register
{
    /// <summary>
    /// 
    /// </summary>
    public class Buffer32 : IRegister
    {
        #region constants

        protected const string REQUESTED_SIZE_TOO_LARGE = "Requested size exceeds max buffer size.";

        #endregion constants

        #region variables

        protected object mResource = new object();
        protected AddressSpace mBar = AddressSpace.PxiBar0;
        protected Int32 mAddr; // BAR0 offset.
        protected Int32 mBufSizeInWords;
        protected Int32 mBufSizeInBytes;
        protected readonly IRegDriver mDriver;
        private readonly string mName;

        protected static ILogger mLogger = LogManager.RootLogger;

        #endregion variables

        #region constructors

        /// <summary>
        /// Creates a 32 bit buffer region
        /// </summary>
        /// <param name="BAR">BAR space the buffer is in</param>
        /// <param name="barOffset">starting byte address in BAR0 space.</param>
        /// <param name="sizeInBytes">buffer size in bytes.</param>
        /// <param name="driver">driver buffer uses to access the buffer.</param>
        /// <param name="name">buffer name.</param>
        public Buffer32( string name, AddressSpace BAR, Int32 barOffset, Int32 sizeInBytes, IRegDriver driver ) :
            this( name, barOffset, sizeInBytes, driver )
        {
            mBar = BAR;
        }

        /// <summary>
        /// Creates a 32 bit buffer region
        /// </summary>
        /// <param name="barOffset">starting byte address in BAR0 space.</param>
        /// <param name="sizeInBytes">buffer size in bytes.</param>
        /// <param name="driver">driver buffer uses to access the buffer.</param>
        /// <param name="name">buffer name.</param>
        public Buffer32( string name, Int32 barOffset, Int32 sizeInBytes, IRegDriver driver )
        {
            mName = name;
            mDriver = driver;
            mAddr = barOffset;
            mBufSizeInWords = sizeInBytes / 4;
            mBufSizeInBytes = sizeInBytes;
            IsApplyEnabled = true;
        }

        #endregion constructors

        public string NameBase
        {
            get
            {
                int i = mName.IndexOf( '_' );
                return ( i > 0 ) ? mName.Substring( i + 1 ) : mName;
            }
        }

        public Int32 Offset
        {
            get
            {
                return mAddr;
            }
        }

        public RegType RegType
        {
            get
            {
                return RegType.Buffer;
            }
        }

        /// <summary>
        /// If IsMemoryMapped==true, this register reads/writes directly to some memory
        /// location (as opposed to interacting with other registers or serial devices).
        /// The main use for this distinction is for refreshing blocks of registers by
        /// performing a single buffer read (see MemoryMappedRegDriver.RegRefresh for
        /// an example coding).
        /// </summary>
        public virtual bool IsMemoryMapped
        {
            get
            {
                // Although most buffers really are memory mapped, we don't want
                // RegRefresh() operating on buffers so...
                return false;
            }
        }

        public Int32 SizeInBytes
        {
            get
            {
                return mBufSizeInBytes;
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
        public int Value32
        {
            get
            {
                // Read the first 32 bits...
                byte[] temp = Read8( 8 );
                return BitConverter.ToInt32( temp, 0 );
            }
            set
            {
                throw new NotImplementedException();
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
        public long Value64
        {
            get
            {
                // Read the first 64 bits...
                byte[] temp = Read8( 8 );
                return BitConverter.ToInt64( temp, 0 );
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Reads a Register value from HW. 
        /// The software copy of the registers value is also updated.
        /// If this is called on a 64 bit register, the implementation may
        /// either throw an exception or truncate the value to 32 bits.
        /// </summary>
        /// <returns></returns>
        public int Read32()
        {
            return Value32;
        }

        /// <summary>
        /// Reads a Register value from HW. 
        /// The software copy of the registers value is also updated.
        /// If this is called on a 32 bit register, the implementation may
        /// either throw an exception or zero pad the value to 64 bits.
        /// </summary>
        /// <returns></returns>
        public long Read64()
        {
            return Value64;
        }

        /// <summary>
        /// Writes a register value to HW.  The software copy of the registers value is updated.
        /// If this is called on a 64 bit register, the implementation may
        /// either throw an exception or zero pad the value to 64 bits.
        /// </summary>
        /// <param name="registerValue">the value to write to HW></param>
        public void Write32( int registerValue )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Writes a register value to HW using the specified IRegDriver.
        /// The software copy of the registers value is also updated.
        /// If this is called on a 64 bit register, the implementation may
        /// either throw an exception or zero pad the value to 64 bits.
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="registerValue">the value to write to HW></param>
        public void Write32( IRegDriver driver, int registerValue )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Writes a register value to HW.  The software copy of the registers value is updated.
        /// If this is called on a 32 bit register, the implementation may
        /// either throw an exception or truncate the value to 32 bits.
        /// </summary>
        /// <param name="registerValue">the value to write to HW></param>
        public void Write64( long registerValue )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Writes a register value to HW using the specified IRegDriver.
        /// The software copy of the registers value is also updated.
        /// If this is called on a 32 bit register, the implementation may
        /// either throw an exception or truncate the value to 32 bits.
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="registerValue">the value to write to HW></param>
        public void Write64( IRegDriver driver, long registerValue )
        {
            throw new NotImplementedException();
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
        public void Push()
        {
            // NOP for buffers...
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
        public void Pop()
        {
            // NOP for buffers...
        }

        /// <summary>
        /// Returns the type (normally an Enum) of the bit field identifiers.  If this register
        /// does not support fields, returns null
        /// </summary>
        public Type BFType
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Return the index of the first BitField.
        /// 
        /// BitFields are internally stored in an array.  "empty" slots contain a null and "non-empty" slots
        /// contain a BitField.  FirstBF is the index of the first non-empty slot.
        /// </summary>
        public int FirstBF
        {
            get
            {
                return -1;
            }
        }

        /// <summary>
        /// Return the index of the last BitField.
        /// 
        /// BitFields are internally stored in an array.  "empty" slots contain a null and "non-empty" slots
        /// contain a BitField.  LastBF is the index of the last non-empty slot.
        /// </summary>
        public int LastBF
        {
            get
            {
                return -1;
            }
        }

        /// <summary>
        /// Return the index of the first BitField.
        /// 
        /// BitFields are internally stored in an array.  "empty" slots contain a null and "non-empty" slots
        /// contain a BitField.  NumBitFields return (LastBF-FirstBF-1) but it is possible for some of the
        /// entries between these values to be null.
        /// </summary>
        public int NumBitFields
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Returns a reference to the BitField specified by index i.
        /// 
        /// Some implementations also provide one of the following methods (not part of this interface)
        ///     RegField32 Field( uint );
        ///     RegField64 Field( uint );
        /// </summary>
        public IBitField GetField( uint i )
        {
            throw new NotImplementedException( "Not supported for Buffer32" );
        }

        /// <summary>
        /// Adds a Bit Field to a Register
        /// </summary>
        /// <param name="bf">The BitField being added to the register</param>
        /// <remarks>The BitField passed in must be an implementation of the Type.BFType
        /// passed in on construction of the register.  If not, an exception is thrown.</remarks>
        public void AddField( IBitField bf )
        {
            throw new NotImplementedException( "Not supported for Buffer32" );
        }

        /// <summary>
        /// For diagnostic purposes -- normally only called in debug/development
        /// 
        /// Iterate over the BitField array and if there are any gaps (i.e. undefined fields)
        /// display a debug message.
        /// 
        /// This may happen if either
        /// 1) The BitField enumeration (BFType) does not assign contiguous values
        /// 2) Not all bit field enumerations are used
        /// </summary>
        /// <remarks> Will print a debug console message if a bitfield is not defined.</remarks>
        public void VerifyBitFields()
        {
            // Doesn't support BitFields ... so NOP
        }

        /// <summary>
        /// Fields exposes the internal collection of BitFields ... This is intended ONLY
        /// for use by RegFactory to dynamically create registers.
        /// </summary>
        public IBitField[] Fields
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.Cmd) != 0
        /// </summary>
        public bool IsCommand
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.Event) != 0
        /// </summary>
        public bool IsEvent
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.WO) != 0
        /// </summary>
        public bool IsWriteOnly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.RO) != 0
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.VolatileRw) != 0
        /// </summary>
        public bool IsVolatileReadWrite
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.NoForce) != 0
        /// </summary>
        public bool IsNoForce
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.NoForce) == 0
        /// </summary>
        public bool IsForceAllowed
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.NoValue) != 0
        /// </summary>
        public bool IsNoValue
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.NoValueFilter) != 0
        /// </summary>
        public bool IsNoValueFilter
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.CannotReadDirectly) != 0
        /// </summary>
        public bool IsCannotReadDirectly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Write Register contents to hardware if dirty (out of sync with Hardware).
        /// </summary>
        /// <param name="forceApply">Write contents even if not dirty.</param>
        public void Apply( bool forceApply )
        {
            // Not supported for RegType.Buffer
            throw new NotImplementedException();
        }

        public void Apply( IRegDriver driver, bool forceApply )
        {
            // Not supported for RegType.Buffer
            throw new NotImplementedException();
        }

        /// <summary>
        /// Enable/disable Apply() operation.  When false, Apply() will have no effect
        /// even if force==true.  The main intent of this method is to allow tuning of
        /// register write sequence.  E.g.
        ///       reg1.IsApplyEnabled = false; // prevent the normal sequence from applying the register
        ///       group.ApplyCommonCarrierReg(...);
        ///       ...
        ///       reg1.IsApplyEnabled = true;  // reenable apply and apply it
        ///       reg1.Apply( driver, force );
        /// </summary>
        public bool IsApplyEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// Does this register require an Apply? (i.e. is it "dirty")
        /// </summary>
        /// <remarks>
        /// The client code should rarely need to set this value -- the internal
        /// management of dirty should be sufficient for all but exceptional cases.
        /// </remarks>
        public bool NeedApply
        {
            get;
            set;
        }

        public void UpdateRegVal()
        {
            // Not supported for RegType.Buffer
            throw new NotImplementedException();
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
        public void UpdateRegVal32( int value )
        {
            // Not supported for RegType.Buffer
            throw new NotImplementedException();
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
        public void UpdateRegVal64( long value )
        {
            // Not supported for RegType.Buffer
            throw new NotImplementedException();
        }

        /// <summary>
        /// GroupDirtyBit is an optional way of tracking if any register in a group of registers
        /// is dirty (i.e. Apply() will write the value).  In the default implementation, RegBase,
        /// if NeedApply is set to true, GroupDirtyBit is set to true.  RegBase will never set
        /// GroupDirtyBit to false (that is intended as a application layer operation).
        /// 
        /// NOTE: a register aggregates a single instance of IDirtyBit so if a register is a member
        ///       of multiple groups it will only set the dirty bit for one of them!
        /// </summary>
        public IDirtyBit GroupDirtyBit
        {
            get;
            set;
        }

        public Int32 Addr
        {
            get
            {
                return mAddr;
            }
        }

        /// <summary>
        /// The IRegDriver this register will use when Apply() is called.  A different IRegDriver may
        /// by used if Apply( IRegDriver, ...) is called.
        /// </summary>
        public IRegDriver Driver
        {
            get
            {
                return mDriver;
            }
        }

        /// <summary>
        /// The 'resource' the register will lock before performing operations.  If a client needs to perform
        /// multi-register operations atomically, execute the code inside:
        /// 
        /// <code>
        ///    lock( IRegister.Resource )
        ///    {
        ///       // atomic code goes here
        ///    }
        /// </code>
        /// 
        /// *** IMPORTANT *** Setting this property should only be done during construction of the system when
        /// no register operations are in progress.  The register constructor will normally create its own
        /// resource, but to simplify locking it is possible to override this by assigning groups of registers
        /// the same resource - namely, when any register in the group sharing the resource is locked they are
        /// all locked.
        /// </summary>
        public object Resource
        {
            get
            {
                return mResource;
            }
            set
            {
                // Insure exclusive access before swapping...
                object oldResource = mResource;
                lock( oldResource )
                {
                    mResource = value;
                }
            }
        }

        public string Name
        {
            get
            {
                return mName;
            }
        }

        public void Write( Int32[] data )
        {
            // Call signature that includes logging...
            Write( null, data, data.Length );
        }

        public virtual void Write( IRegDriver driver, Int32[] data, int length )
        {
            if( length > mBufSizeInWords )
            {
                throw new ApplicationException( REQUESTED_SIZE_TOO_LARGE );
            }

            if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
            {
                mLogger.LogAppend( new RegisterLoggingEvent( LogLevel.Fine, data.Length, this )
                    {
                        Operation =
                            ( driver == null || driver.IsRecordingSession == false )
                                ? RegisterLoggingEvent.OperationType.BufWr32s
                                : RegisterLoggingEvent.OperationType.BufWr32sCS
                    } );
                if( mLogger.IsLoggingEnabledFor( LogLevel.Finest ) )
                {
                    LogBuffer32( "Write Buffer32[]=", data, 0, length );
                }
            }

            if( driver != null )
            {
                driver.ArrayWrite( mAddr, data, length );
            }
            else
            {
                mDriver.ArrayWrite( mAddr, data, length );
            }
        }

        public virtual void Write( IRegDriver driver, Int32[] data, int length, int offset )
        {
            if( length > mBufSizeInWords )
            {
                throw new ApplicationException( REQUESTED_SIZE_TOO_LARGE );
            }

            if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
            {
                mLogger.LogAppend( new RegisterLoggingEvent( LogLevel.Fine, data.Length, this )
                    {
                        Operation =
                            ( driver == null || driver.IsRecordingSession == false )
                                ? RegisterLoggingEvent.OperationType.BufWr32s
                                : RegisterLoggingEvent.OperationType.BufWr32sCS
                    } );
                if( mLogger.IsLoggingEnabledFor( LogLevel.Finest ) )
                {
                    LogBuffer32( "Write Buffer32[]=", data, offset, length );
                }
            }

            if( driver != null )
            {
                driver.ArrayWrite( mAddr, data, length, offset );
            }
            else
            {
                mDriver.ArrayWrite( mAddr, data, length, offset );
            }
        }


        public void Write( IRegDriver driver, Int32[] data )
        {
            // Call signature that includes logging...
            Write( driver, data, data.Length );
        }

        public void Write( byte[] data )
        {
            // Call signature that includes logging...
            Write( data, 0, data.Length );
        }


        /// <summary>
        /// Write a byte array to the buffer on the default register driver.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        public virtual void Write( byte[] data, int startIndex, int length )
        {
            Write( null, data, startIndex, length );
        }

        /// <summary>
        /// Write a byte array to the buffer on a specified driver.
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="data"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        public virtual void Write( IRegDriver driver, byte[] data, int startIndex, int length )
        {
            if( length > mBufSizeInBytes )
            {
                throw new ApplicationException( REQUESTED_SIZE_TOO_LARGE );
            }

            if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
            {
                mLogger.LogAppend( new RegisterLoggingEvent( LogLevel.Fine, length, this )
                {
                    Operation = RegisterLoggingEvent.OperationType.BufWr8s
                } );
                if( mLogger.IsLoggingEnabledFor( LogLevel.Finest ) )
                {
                    LogBuffer8( "Write Buffer8[]=", data, startIndex, length );
                }
            }

            if( driver == null )
            {
                mDriver.ArrayWrite( mAddr, data, startIndex, length );
            }
            else
            {
                driver.ArrayWrite( mAddr, data, startIndex, length );
            }
        }


        public virtual void Read( byte[] data8, int index, int numBytesToRead )
        {
            if( numBytesToRead > mBufSizeInBytes )
            {
                throw new ApplicationException( REQUESTED_SIZE_TOO_LARGE );
            }

            if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
            {
                mLogger.LogAppend( new RegisterLoggingEvent( LogLevel.Fine, numBytesToRead, this )
                    {
                        Operation = RegisterLoggingEvent.OperationType.BufRd8s_beg
                    } );
            }

            if( mDriver != null ) // in case module is not plugged in and we create it in ram.
            {
                mDriver.ArrayRead( mAddr, ref data8, index, numBytesToRead );
            }

            if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
            {
                if( mLogger.IsLoggingEnabledFor( LogLevel.Finest ) )
                {
                    LogBuffer8( "READ Buffer8[]=", data8, index, numBytesToRead );
                }
                mLogger.LogAppend( new RegisterLoggingEvent( LogLevel.Fine, numBytesToRead, this )
                    {
                        Operation = RegisterLoggingEvent.OperationType.BufRd8s_end
                    } );
            }
        }

        public void ReadFifo( byte[] data8, int index, int numBytesToRead )
        {
            if( numBytesToRead > mBufSizeInBytes )
            {
                throw new ApplicationException( REQUESTED_SIZE_TOO_LARGE );
            }

            if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
            {
                mLogger.LogAppend( new RegisterLoggingEvent( LogLevel.Fine, numBytesToRead, this )
                    {
                        Operation = RegisterLoggingEvent.OperationType.BufRd8s_beg
                    } );
            }

            if( mDriver != null ) // in case module is not plugged in and we create it in ram.
            {
                mDriver.ReadFifo( mBar, mAddr, numBytesToRead, data8, index );
            }

            if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
            {
                if( mLogger.IsLoggingEnabledFor( LogLevel.Finest ) )
                {
                    LogBuffer8( "READ Buffer8[]=", data8, index, numBytesToRead );
                }
                mLogger.LogAppend( new RegisterLoggingEvent( LogLevel.Fine, numBytesToRead, this )
                    {
                        Operation = RegisterLoggingEvent.OperationType.BufRd8s_end
                    } );
            }
        }

        public Byte[] Read8( int numBytes )
        {
            if( numBytes > mBufSizeInBytes )
            {
                throw new ApplicationException( REQUESTED_SIZE_TOO_LARGE );
            }

            if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
            {
                mLogger.LogAppend( new RegisterLoggingEvent( LogLevel.Fine, numBytes, this )
                    {
                        Operation = RegisterLoggingEvent.OperationType.BufRd8s_beg
                    } );
            }


            byte[] data = mDriver.ArrayRead8( mAddr, numBytes );

            if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
            {
                if( mLogger.IsLoggingEnabledFor( LogLevel.Finest ) )
                {
                    LogBuffer8( "READ Buffer8[]=", data, 0, numBytes );
                }
                mLogger.LogAppend( new RegisterLoggingEvent( LogLevel.Fine, numBytes, this )
                    {
                        Operation = RegisterLoggingEvent.OperationType.BufRd8s_end
                    } );
            }

            return data;
        }


        public Int32[] Read( int num32BitWords )
        {
            if( num32BitWords > mBufSizeInWords )
            {
                throw new ApplicationException( REQUESTED_SIZE_TOO_LARGE );
            }

            if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
            {
                mLogger.LogAppend( new RegisterLoggingEvent( LogLevel.Fine, num32BitWords, this )
                    {
                        Operation = RegisterLoggingEvent.OperationType.BufRd32s_beg
                    } );
            }

            Int32[] data = mDriver.ArrayRead32( mAddr, num32BitWords );

            if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
            {
                if( mLogger.IsLoggingEnabledFor( LogLevel.Finest ) )
                {
                    LogBuffer32( "READ Buffer32[]=", data, 0, num32BitWords );
                }
                mLogger.LogAppend( new RegisterLoggingEvent( LogLevel.Fine, num32BitWords, this )
                    {
                        Operation = RegisterLoggingEvent.OperationType.BufRd32s_end
                    } );
            }

            return data;
        }

        /// <summary>
        /// Utility method to dump formatted array/buffer contents into the log file.
        /// </summary>
        /// <param name="description">added at the start up the dump</param>
        /// <param name="data">the data to dump</param>
        /// <param name="offset">offset into data</param>
        /// <param name="count">number of bytes to dump</param>
        public static void LogBuffer8( string description, byte[] data, int offset, int count )
        {
            StringBuilder buffer = new StringBuilder( description );
            int n = Math.Min( count, 16 );
            for( int j = 0; j < n; j++ )
            {
                buffer.AppendFormat( "0x{0:x2},", data[ offset + j ] );
            }
            if( n < count )
            {
                buffer.Append( "..." );
            }
            mLogger.LogAppend( new LoggingEvent( LogLevel.Finest, buffer.ToString() ) );
        }

        /// <summary>
        /// Utility method to dump formatted array/buffer contents into the log file.
        /// </summary>
        /// <param name="description">added at the start up the dump</param>
        /// <param name="data">the data to dump</param>
        /// <param name="offset">offset into data</param>
        /// <param name="count">number of words to dump</param>
        public static void LogBuffer32( string description, int[] data, int offset, int count )
        {
            StringBuilder buffer = new StringBuilder( description );
            int n = Math.Min( count, 16 );
            for( int j = 0; j < n; j++ )
            {
                buffer.AppendFormat( "0x{0:x8},", data[ offset + j ] );
            }
            if( n < count )
            {
                buffer.Append( "..." );
            }
            mLogger.LogAppend( new LoggingEvent( LogLevel.Finest, buffer.ToString() ) );
        }

        #region Register bit locking methods

        /// <summary>
        /// Prevent register bits from being modified.
        /// </summary>
        /// <param name="lockMask">The desired mask of locked bits></param>
        public void LockBits32(Int32 lockMask)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Prevent register bits from being modified.
        /// </summary>
        /// <param name="lockMask">The desired mask of locked bits></param>
        public void LockBits64(Int64 lockMask)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Prevent register bits from being modified.
        /// </summary>
        /// <param name="startBit">Starting bit to lock></param>
        /// <param name="bitCount">Number of bits to lock></param>
        public void LockBits(int startBit, int bitCount)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Prevent a register from being modified.
        /// </summary>
        public void LockBits()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Allow register bits to be modified.
        /// </summary>
        /// <param name="startBit">Starting bit to unlock></param>
        /// <param name="bitCount">Number of bits to unlock></param>
        public void UnlockBits(int startBit, int bitCount)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Allow a register to be modified.
        /// </summary>
        public void UnlockBits()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Query a register's lock mask.
        /// </summary>
        /// <returns></returns>
        public Int32 GetLockMask32()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Query a register's lock mask.
        /// </summary>
        /// <returns></returns>
        public Int64 GetLockMask64()
        {
            throw new NotImplementedException();
        }

        #endregion Register bit locking methods
    }
}