using InstrumentDriver.Core.Common.IO;
using System;
using System.Collections.Generic;

namespace InstrumentDriver.Core.Mock
{
    /// <summary>
    /// MockSession is an implementation of ISession useful for unit tests and simulation.
    /// </summary>
    public class MockSession : ISession
    {
        #region Delegates

        /// <summary>
        /// WriteNotifyDelegate, set via WriteNotify, is called whenever a "register" write occurs
        /// i.e. when Out32( offset, value ) or Out64( offset, value) are called.  This allows
        /// a client (normally unit test or simulation) to dynamically change the hardware
        /// emulation (normally other register/array values) based on the value written to
        /// a particular register.  For example
        ///    ...
        ///    driver.WriteNotify = MyWriteNotifyDelegate;
        ///    ...
        ///    void MyWriteNotifyDelegate( MockSession session, int barOffset, object arg ) {
        ///        if( barOffset == SOME_OFFSET ) {
        ///           long value = (long)arg;
        ///           switch( value ) {
        ///           case SOME_VALUE:  session.Memory[ANOTHER_OFFSET] = ANOTHER_VALUE; break;
        ///        ...
        ///    }
        /// </summary>
        /// <param name="session"></param>
        /// <param name="bar"></param>
        /// <param name="barOffset"></param>
        /// <param name="arg"></param>
        public delegate void WriteNotifyDelegate( MockSession session, short bar, int barOffset, object arg );

        #endregion Delegates

        #region member variables

        private readonly Dictionary <int, int> mAttributes = new Dictionary <int, int>();
        private readonly Dictionary <int, int> mMemory = new Dictionary <int, int>();
        private readonly Dictionary <long, byte[]> mBuffer8 = new Dictionary <long, byte[]>();
        private readonly Dictionary <long, int[]> mBuffer32 = new Dictionary <long, int[]>();
        private AgVisa32.viEventHandler mEventHandler;
        private int mEventHandlerType;
        private int mEventHandlerParm;
        private int mDefaultTimeout = 5000; // ms

        #endregion member variables

        /// <summary>
        /// Construct a MockSession with the default values for
        /// ...ModelCode = 0x1239 
        /// ...ModelName = MOCK
        /// ...SlotNumber = 5
        /// </summary>
        public MockSession()
        {
            // MockSession is always simulated...
            IsSimulated = true;
            ModelCode = 0x1239; 
            ModelName = "MOCK";
            SlotNumber = 5;
        }

        public MockSession( short modelCode, string modelName, short slotNumber )
        {
            // MockSession is always simulated...
            IsSimulated = true;
            ModelCode = modelCode;
            ModelName = modelName;
            SlotNumber = slotNumber;
        }

        /// <summary>
        /// Execute the VISA callback.
        /// 
        /// NOTE: to better emulate hardware (which you should do for any complex emulation) you should
        ///       call this method from a above normal priority worker thread (i.e. NOT the thread that
        ///       called the I/O method)
        /// </summary>
        /// <param name="context"></param>
        public void FireEventHandler( int context )
        {
            if( mEventHandler != null )
            {
                mEventHandler( SessionID, mEventHandlerType, context, mEventHandlerParm );
            }
        }

        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
        }

        #endregion Implementation of IDisposable

        #region Hardware Emulation

        /// <summary>
        /// WriteNotify sets WriteNotifyDelegate which is called whenever a "register" write occurs
        /// i.e. when Out32( offset, value ) or Out64( offset, value) are called.  This allows
        /// a client (normally unit test or simulation) to dynamically change the hardware
        /// emulation (normally other register/array values) based on the value written to
        /// a particular register.  For example
        ///    ...
        ///    driver.WriteNotify = MyWriteNotifyDelegate;
        ///    ...
        ///    void MyWriteNotifyDelegate( MockSession session, int barOffset, int value ) {
        ///        if( barOffset == SOME_OFFSET ) {
        ///           switch( value ) {
        ///           case SOME_VALUE:  session.Memory[ANOTHER_OFFSET] = ANOTHER_VALUE; break;
        ///        ...
        ///    }
        /// </summary>
        public WriteNotifyDelegate WriteNotify
        {
            set;
            get;
        }

        /// <summary>
        /// Memory is a dictionary of offset/value pairs used as the "backing store" for a
        /// mock session.  Reads and writes of single values (not arrays) will access Memory
        /// so that by default any value written will persist.  This behavior may be changed
        /// by using WriteNotifyDelegate (see WriteNotify)
        /// </summary>
        public Dictionary <int, int> Memory
        {
            get
            {
                return mMemory;
            }
        }

        /// <summary>
        /// Buffer32 is a dictionary of offset/array pairs used as the "backing store" for a
        /// mock session.  Reads and writes of 32 bit arrays will access Buffer32 so that by
        /// default any value written will persist.  This behavior may be changed by using
        /// WriteNotifyDelegate (see WriteNotify)
        /// </summary>
        public Dictionary <long, int[]> Buffer32
        {
            get
            {
                return mBuffer32;
            }
        }

        /// <summary>
        /// Buffer8 is a dictionary of offset/array pairs used as the "backing store" for a
        /// mock session.  Reads and writes of 8 bit arrays will access Buffer8 so that by
        /// default any value written will persist.  This behavior may be changed by using
        /// WriteNotifyDelegate (see WriteNotify)
        /// </summary>
        public Dictionary <long, byte[]> Buffer8
        {
            get
            {
                return mBuffer8;
            }
        }

        #endregion Hardware Emulation

        #region Implementation of ISession

        /// <summary>
        /// Return the underlying session ID ... the interpretation/use of the ID
        /// depends on the specific instantiation
        /// </summary>
        public int SessionID
        {
            get;
            private set;
        }

        /// <summary>
        /// Returns the resource descriptor used by Open().   Only valid after Open()
        /// has been called.
        /// </summary>
        public string ResourceDescriptor
        {
            get;
            private set;
        }

        /// <summary>
        /// Open the underlying driver (VISA, Eiger, whatever)
        /// </summary>
        /// <param name="resource">the VISA resource descriptor.  Ignored if simulated is true</param>
        /// <param name="timeout">I/O timeout in ms ... not all drivers/sessions use this</param>
        /// <param name="simulated">if true, no connection to hardware will be made</param>
        public void Open( string resource, int timeout, bool simulated )
        {
            Open( resource, timeout, simulated, true );
        }

        /// <summary>
        /// Open the underlying driver (VISA, Eiger, whatever)
        /// </summary>
        /// <param name="resource">the VISA resource descriptor.  Ignored if simulated is true</param>
        /// <param name="timeout">I/O timeout in ms ... not all drivers/sessions use this</param>
        /// <param name="simulated">if true, no connection to hardware will be made</param>
        /// <param name="exclusive">if true opens session exclusively (VI_EXCLUSIVE_LOCK).  if false opens session with no lock (VI_NO_LOCK)</param>
        public void Open( string resource, int timeout, bool simulated, bool exclusive )
        {
            // Fake session id...
            SessionID = resource.Length;
            ResourceDescriptor = resource;
            Timeout = timeout;
            mDefaultTimeout = timeout;
        }

        /// <summary>
        /// Reset any "volatile" settings to a default state.  Typical settings affected
        /// by this are
        /// * timeout
        /// * cached memory buffers
        /// </summary>
        public void Reset()
        {
            // Reset timeout...
            Timeout = mDefaultTimeout;
        }

        /// <summary>
        /// Close the underlying driver
        /// </summary>
        public void Close()
        {
            SessionID = 0;
            mAttributes.Clear();
            mMemory.Clear();
        }

        /// <summary>
        /// The current simulation state
        /// </summary>
        public bool IsSimulated
        {
            get;
            set;
        }

        /// <summary>
        /// Lock the underlying driver.
        /// 
        /// If the underlying driver requires a "key" (such as VISA's viLock()), use the
        /// value returned by LockSession() of the first session locked.  If this is the
        /// first call to LockSession for this shared session, the value is the suggested
        /// value for the key (but the implementation may ignore this and return a
        /// different value).
        /// </summary>
        /// <param name="exclusive">true (VI_EXCLUSIVE_LOCK), false (VI_SHARED_LOCK)</param>
        /// <param name="timeout">timeout in ms ... not all drivers/sessions use this</param>
        /// <param name="key">"authorization" to lock this visa session.</param>
        /// <returns>authorization for other calls to LockSession of a shared session</returns>
        public string LockSession( bool exclusive, int timeout, string key )
        {
            IsSessionLocked = true;
            return key;
        }

        /// <summary>
        /// Unlock the underlying driver.  Driver may have been locked by LockSession() or
        /// if opened exclusively by Open()
        /// </summary>
        public void UnlockSession()
        {
            IsSessionLocked = false;
        }

        /// <summary>
        /// Return the lock status of the session.  This does not distinguish between
        /// shared and exclusive locks.  Most implementations simply set a flag in
        /// LockSession or Open and clear it in UnlockSession (i.e. the implementation
        /// may not query the underlying driver)
        /// </summary>
        public bool IsSessionLocked
        {
            get;
            private set;
        }

        /// <summary>
        /// The default timeout of the session. I/O operations that do not explicitly
        /// specify a timeout will use this value.  Initially set by the timeout
        /// parameter used in Open()
        /// </summary>
        public int Timeout
        {
            get;
            set;
        }


        public void InstallHandler( VisaEvents eventType, AgVisa32.viEventHandler eventHandler, int parm )
        {
            mEventHandler = eventHandler;
            mEventHandlerParm = parm;
            mEventHandlerType = (int)eventType;
        }

        public void UninstallHandler( VisaEvents eventType, AgVisa32.viEventHandler eventHandler, int parm )
        {
            if( ReferenceEquals( mEventHandler, eventHandler ) )
            {
                mEventHandler = null;
            }
        }

        public void EnableEvent( VisaEvents eventType, EventMechanism mechanism )
        {
        }

        public void DisableEvent( VisaEvents eventType, EventMechanism mechanism )
        {
        }

        public void WaitOnEvent( VisaEvents eventType, int timeout )
        {
        }

        /// <summary>
        /// Get the specified attribute
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public int GetSessionAttribute( int attribute )
        {
            return ( mAttributes.ContainsKey( attribute ) ) ? mAttributes[ attribute ] : 0;
        }

        /// <summary>
        /// Set the specified attribute
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetSessionAttribute( int attribute, int value )
        {
            mAttributes[ attribute ] = value;
        }

        /// <summary>
        /// Perform an input
        /// </summary>
        /// <param name="BAR"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public int In32( short BAR, int offset )
        {
            return ( mMemory.ContainsKey( offset ) ) ? mMemory[ offset ] : 0;
        }

        /// <summary>
        /// Perform an output
        /// </summary>
        /// <param name="BAR"></param>
        /// <param name="offset"></param>
        /// <param name="value"></param>
        public void Out32( short BAR, int offset, int value )
        {
            mMemory[ offset ] = value;
            // Optionally notify clients
            if( WriteNotify != null )
            {
                WriteNotify( this, BAR, offset, value );
            }
        }

        public void Out64( short bar, long barOffset, long writeValue )
        {
            // TODO
        }

        public void MoveOut64( short bar, long barOffset, int length, long[] dataArray )
        {
            // TODO
        }

        public void MoveOut32PageAligned(short bar, long barOffset, int length, int[] dataArray, int dataOffset = 0)
        {
            // Delegate to MoveOut32
            MoveOut32(bar, barOffset, length, dataArray, dataOffset);
        }

        public void MoveOut32(short bar, long barOffset, int length, int[] dataArray)
        {
            int[] subset = new int[length];
            Array.Copy(dataArray, 0, subset, 0, length);
            Buffer32[barOffset] = subset;
            if (WriteNotify != null)
            {
                WriteNotify(this, bar, (int)barOffset, dataArray);
            }
        }

        public void MoveOut32( short bar, long barOffset, int length, int[] dataArray, int  dataOffset )
        {
            int[] subset = new int[length];
            Array.Copy(dataArray, dataOffset, subset, 0, length);
            Buffer32[ barOffset ] = subset;
            if( WriteNotify != null )
            {
                WriteNotify( this, bar, (int)barOffset, dataArray );
            }
        }

        public void MoveOut8( short bar, long barOffset, int length, byte[] dataArray )
        {
            byte[] subset = new byte[length];
            Array.Copy( dataArray, 0, subset, 0, length );
            Buffer8[ barOffset ] = subset;
        }

        public long In64( short bar, long barOffset )
        {
            // TODO
            return 0;
        }

        public long[] MoveIn64( short bar, long barOffset, int numToRead )
        {
            // TODO
            return new long[numToRead];
        }

        public int[] MoveIn32( short bar, long barOffset, int numToRead )
        {
            int[] result = new int[numToRead];
            // Delegate the rest to MoveIn32
            MoveIn32( bar, barOffset, result, numToRead );
            return result;
        }

        public int[] MoveIn32PageAligned( short bar, long barOffset, int numToRead )
        {
            // Delegate to MoveIn32
            return MoveIn32( bar, barOffset, numToRead );
        }

        public void MoveIn32PageAligned( short bar, long barOffset, int numToRead, int[] inData, int offset )
        {
            // A buffer takes priority over individual memory values
            if( Buffer32.ContainsKey( barOffset ) )
            {
                int[] contents = Buffer32[ barOffset ];
                Array.Copy( contents, 0, inData, offset, Math.Min( contents.Length - offset, numToRead ) );
            }
            else
            {
                // return individual memory values that fall in the range
                long finalOffset = barOffset + ( ( numToRead - 1 ) * sizeof( int ) );
                foreach( var key in mMemory.Keys )
                {
                    if( barOffset <= key && key <= finalOffset )
                    {
                        int index = offset + (int)( key - barOffset ) / sizeof( int );
                        inData[ index ] = mMemory[ key ];
                    }
                }
            }
        }

        public void MoveIn32( short bar, long barOffset, int[] data, int numToRead )
        {
            // A buffer takes priority over individual memory values
            if( Buffer32.ContainsKey( barOffset ) )
            {
                int[] contents = Buffer32[ barOffset ];
                Array.Copy( contents, data, Math.Min( contents.Length, numToRead ) );
            }
            else
            {
                // return individual memory values that fall in the range
                long finalOffset = barOffset + ( numToRead - 1 ) * sizeof( int );
                foreach( var key in mMemory.Keys )
                {
                    if( barOffset <= key && key <= finalOffset )
                    {
                        int index = (int)( key - barOffset ) / sizeof( int );
                        data[ index ] = mMemory[ key ];
                    }
                }
            }
        }

        public byte[] MoveIn8( short bar, long barOffset, int numBytesToRead )
        {
            byte[] result = new byte[numBytesToRead];
            if( Buffer8.ContainsKey( barOffset ) )
            {
                byte[] contents = Buffer8[ barOffset ];
                Array.Copy( contents, result, Math.Min( contents.Length, numBytesToRead ) );
            }
            return result;
        }

        public void MoveIn8PageAligned( short bar, long barOffset, int numToRead, byte[] inData, int offset )
        {
            if( Buffer8.ContainsKey( barOffset ) )
            {
                byte[] contents = Buffer8[ barOffset ];
                Array.Copy( contents, 0, inData, offset, Math.Min( contents.Length - offset, numToRead ) );
            }
        }

        /// <summary>
        /// This operation maps in a specified memory space. The memory space that is mapped is dependent on the
        /// mapSpace (refer to the following table) parameter. The address parameter returns the address in your
        /// process space where memory is mapped. 
        /// 
        /// Value               Description
        /// ------------        -------------------------
        /// VI_A16_SPACE        Map the A16 address space of VXI/MXI bus.
        /// VI_A24_SPACE        Map the A24 address space of VXI/MXI bus.
        /// VI_A32_SPACE        Map the A32 address space of VXI/MXI bus.
        /// VI_A64_SPACE        Map the A64 address space of VXI/MXI bus.
        /// VI_PXI_CFG_SPACE    Address the PCI configuration space.
        /// VI_PXI_BAR0_SPACE – VI_PXI_BAR5_SPACE	Address the specified PCI memory or I/O space.
        /// VI_PXI_ALLOC_SPACE  Access physical locally allocated memory
        /// </summary>
        /// <param name="mapSpace">Specifies the address space to map. </param>
        /// <param name="mapOffset">Offset (in bytes) of the memory to be mapped.</param>
        /// <param name="mapSize">Amount of memory to map (in bytes).</param>
        /// <param name="accMode">VI_FALSE</param>
        /// <param name="suggested">If suggested parameter is not VI_NULL, the operating system attempts to map 
        ///                         the memory to the address specified in suggested. There is no guarantee,
        ///                         however, that the memory will be mapped to that address. This operation
        ///                         may map the memory into an address region different from suggested.</param>
        /// <param name="address">Address in your process space where the memory was mapped.</param>
        public void MapAddress( short mapSpace,
                                int mapOffset,
                                int mapSize,
                                short accMode,
                                IntPtr suggested,
                                out IntPtr address )
        {
            // Must return 0 (so the Peek/Poke methods can delegate to In/Out)
            address = (IntPtr)0;
        }

        /// <summary>
        /// This operation unmaps the region previously mapped by the MapAddress() operation.
        /// </summary>
        public void UnmapAddress()
        {
            // NOP
        }

        /// <summary>
        /// This operation reads an 8-bit value from the address location specified in addr. The address
        /// must be a valid memory address in the current process mapped by a previous MapAddress() call.
        /// </summary>
        /// <param name="addr">Specifies the source address to read the value.</param>
        /// <returns></returns>
        public byte Peek8( IntPtr addr )
        {
            return 0;
        }

        /// <summary>
        /// This operation reads an 16-bit value from the address location specified in addr. The address
        /// must be a valid memory address in the current process mapped by a previous MapAddress() call.
        /// </summary>
        /// <param name="addr">Specifies the source address to read the value.</param>
        /// <returns></returns>
        public short Peek16( IntPtr addr )
        {
            return 0;
        }

        /// <summary>
        /// This operation reads an 32-bit value from the address location specified in addr. The address
        /// must be a valid memory address in the current process mapped by a previous MapAddress() call.
        /// </summary>
        /// <param name="addr">Specifies the source address to read the value.</param>
        /// <returns></returns>
        public int Peek32( IntPtr addr )
        {
            return In32( 0, addr.ToInt32() );
        }

        /// <summary>
        /// This operation reads an 32-bit value from the address location specified in addr. The address
        /// must be a valid memory address in the current process mapped by a previous MapAddress() call.
        /// </summary>
        /// <param name="addr">Specifies the source address to read the value.</param>
        /// <returns></returns>
        public long Peek64( IntPtr addr )
        {
            return In64( 0, addr.ToInt32() );
        }

        /// <summary>
        /// This operation takes an 8-bit value and stores its content to the address location specified
        /// in addr. The address must be a valid memory address in the current process mapped by a previous
        /// MapAddress() call.
        /// </summary>
        /// <param name="addr">Specifies the destination address to store the value.</param>
        /// <param name="value">data to write</param>
        public void Poke8( IntPtr addr, byte value )
        {
        }

        /// <summary>
        /// This operation takes an 16-bit value and stores its content to the address location specified
        /// in addr. The address must be a valid memory address in the current process mapped by a previous
        /// MapAddress() call.
        /// </summary>
        /// <param name="addr">Specifies the destination address to store the value.</param>
        /// <param name="value">data to write</param>
        public void Poke16( IntPtr addr, short value )
        {
        }

        /// <summary>
        /// This operation takes an 32-bit value and stores its content to the address location specified
        /// in addr. The address must be a valid memory address in the current process mapped by a previous
        /// MapAddress() call.
        /// </summary>
        /// <param name="addr">Specifies the destination address to store the value.</param>
        /// <param name="value">data to write</param>
        public void Poke32( IntPtr addr, int value )
        {
            // Delegate to the corresponding Out
            Out32( 0, addr.ToInt32(), value );
        }

        /// <summary>
        /// This operation takes an 64-bit value and stores its content to the address location specified
        /// in addr. The address must be a valid memory address in the current process mapped by a previous
        /// MapAddress() call.
        /// </summary>
        /// <param name="addr">Specifies the destination address to store the value.</param>
        /// <param name="value">data to write</param>
        public void Poke64( IntPtr addr, long value )
        {
        }

        public string ModelName
        {
            get;
            private set;
        }

        public short ModelCode
        {
            get;
            private set;
        }

        public short SlotNumber
        {
            get;
            private set;
        }

        /// <summary>
        /// Enable/disable use of DMA. For VISA implementations this corresponds to VI_ATTR_DMA_ALLOW_EN
        /// </summary>
        public bool DmaEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// Set/get the DMA source increment. For VISA implementations this corresponds to VI_ATTR_SRC_INCREMENT
        /// </summary>
        public int DmaSourceIncrement
        {
            get;
            set;
        }

        /// <summary>
        /// Set/get the DMA destination increment. For VISA implementations this corresponds to VI_ATTR_DEST_INCREMENT
        /// </summary>
        public int DmaDestinationIncrement
        {
            get;
            set;
        }

        /// <summary>
        /// Set/get the DMA read threshold. For VISA implementations this corresponds to VI_AGATTR_DMA_READ_THRESHOLD
        /// </summary>
        public int DmaReadThreshold
        {
            get;
            set;
        }

        /// <summary>
        /// Set/get the DMA write threshold. For VISA implementations this corresponds to VI_AGATTR_DMA_WRITE_THRESHOLD
        /// </summary>
        public int DmaWriteThreshold
        {
            get;
            set;
        }

        /// <summary>
        /// Set/get the DMA channel. For VISA implementations this corresponds to VI_AGATTR_DMA_CHANNEL
        /// </summary>
        public int DmaChannel
        {
            get;
            set;
        }

        #endregion Implementation of ISession
    }
}