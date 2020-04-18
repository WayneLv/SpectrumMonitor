using System;
using System.Collections.Generic;
using System.Threading;
using InstrumentDriver.Core.Common.IO;
using InstrumentDriver.Core.Register;

namespace InstrumentDriver.Core.Mock
{
    public class MockRegDriver : IRegDriver
    {
        #region Delegates

        /// <summary>
        /// WriteNotifyDelegate, set via WriteNotify, is called whenever a write occurs.
        /// This allows a client (normally a unit test) to dynamically change the hardware
        /// emulation (normally other register/array values) based on the value written to
        /// a particular register.  For example
        ///    ...
        ///    driver.WriteNotify = MyWriteNotifyDelegate;
        ///    ...
        ///    void MyWriteNotifyDelegate( MockRegDriver driver, int barOffset, int value ) {
        ///        if( barOffset == SOME_OFFSET ) {
        ///           switch( value ) {
        ///           case SOME_VALUE:  driver.Hardware[ANOTHER_OFFSET] = ANOTHER_VALUE; break;
        ///        ...
        ///    }
        /// This is called from RegWrite (both 32 and 64 bit values)
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="barOffset"></param>
        /// <param name="value"></param>
        public delegate void WriteNotifyDelegate( MockRegDriver driver, int barOffset, long value );

        #endregion Delegates

        #region variables

        private AddressSpace mAddressSpace;
        private ISession mActiveSession;
        private readonly Dictionary <int, long> mHardware = new Dictionary <int, long>();
        private readonly Dictionary <int, byte[]> mHardwareArray8 = new Dictionary <int, byte[]>();
        private readonly Dictionary <int, int[]> mHardwareArray32 = new Dictionary <int, int[]>();

        #endregion variables

        #region constructors

        public MockRegDriver() : this( new ISession[] { new MockSession() } )
        {
        }

        public MockRegDriver( ISession[] sessions )
        {
            Resource = new Mutex();
            Sessions = sessions;
            AddressSpace = AddressSpace.PxiBar0;
            Reset();
        }

        #endregion constructors

        #region "Hardware emulation" so tests can verify reads & writes

        public void Reset()
        {
            mHardware.Clear();
            ReadCount = 0;
            WriteCount = 0;
            // Some settings to keep software happy
            const int fpgaTypeAddress = 0x448;
            mHardware[ fpgaTypeAddress ] = (int)12345678;
        }

        public Dictionary <int, long> Hardware
        {
            get
            {
                return mHardware;
            }
        }

        public Dictionary <int, byte[]> HardwareArray8
        {
            get
            {
                return mHardwareArray8;
            }
        }

        public Dictionary <int, int[]> HardwareArray32
        {
            get
            {
                return mHardwareArray32;
            }
        }

        public int ReadCount
        {
            get;
            set;
        }

        public int WriteCount
        {
            get;
            set;
        }

        public WriteNotifyDelegate WriteNotify
        {
            get;
            set;
        }

        #endregion

        #region Implementation of IRegDriver

        /// <summary>
        /// The synchronization object used to control access to IRegDriver.
        /// NOTE: As of 25-Oct-2012 this is ineffective ... it was copied from APeX implementation
        ///       which only accessed the Mutex in very few locations (which effectively means it
        ///       does NOT insure exclusive access...
        /// </summary>
        public Mutex Resource
        {
            get;
            private set;
        }

        /// <summary>
        /// The ISession(s) objects used for I/O.  Sessions[0] is the session
        /// for KtVisa32.VI_PXI_BAR0_SPACE, Sessions[1] is the session for
        /// KtVisa32.VI_PXI_BAR1_SPACE, etc.
        ///       
        /// NOTE: empirically, multi-threaded access to multiple BARs requires a separate session
        ///       instance per BAR. The M9214A (Digitizer) sees memory corruption errors during VISA
        ///       calls if a register write occurs in BAR0 while data is being read from BAR2
        /// </summary>
        public ISession[] Sessions
        {
            get;
            set;
        }

        /// <summary>
        /// Return the ISession object for the specified BAR (Base Address Register).
        ///       
        /// NOTE: empirically, multi-threaded access to multiple BARs requires a separate session
        ///       instance per BAR. The M9214A (Digitizer) sees memory corruption errors during VISA
        ///       calls if a register write occurs in BAR0 while data is being read from BAR2
        /// </summary>
        /// <param name="bar"></param>
        /// <returns></returns>
        public ISession Session( AddressSpace bar )
        {
            return Sessions[ (int)bar - (int)AddressSpace.PxiBar0 ];
        }

        /// <summary>
        /// Return the ISession object corresponding to the current value of AddressSpace
        /// This is the same as:   return Session(AddressSpace);
        ///       
        /// NOTE: empirically, multi-threaded access to multiple BARs requires a separate session
        ///       instance per BAR. The M9214A (Digitizer) sees memory corruption errors during VISA
        ///       calls if a register write occurs in BAR0 while data is being read from BAR2
        /// </summary>
        public ISession ActiveSession
        {
            get
            {
                return mActiveSession;
            }
        }

        /// <summary>
        /// Get/set the address space (e.g. BAR0, BAR1, ...) that I/O operations
        /// will use.  This also selects which ISession object is used.  E.g.
        ///    Session( AddressSpace ).Out32( (short)AddressSpace, BARoffset, value );
        ///       
        /// NOTE: empirically, multi-threaded access to multiple BARs requires a separate session
        ///       instance per BAR. The M9214A (Digitizer) sees memory corruption errors during VISA
        ///       calls if a register write occurs in BAR0 while data is being read from BAR2
        /// </summary>
        public AddressSpace AddressSpace
        {
            get
            {
                return mAddressSpace;
            }
            set
            {
                int index = (int)value - (int)AddressSpace.PxiBar0;
                mAddressSpace = value;
                mActiveSession = Sessions[ index ];
                InternalBAR = index;
            }
        }

        /// <summary>
        /// InternalBAR is the "internal" VISA representation of VI_ATTR_PXI_MEM_BASE_BARx (i.e. the
        /// value returned by GetSessionAttribute( KtVisa32.VI_ATTR_PXI_MEM_BASE_BARx ) for a specific
        /// session).  This value is used ONLY by control streams / peer-2-peer forwarding.  All
        /// VISA calls should use the VISA enumeration and NOT this property.  This value is updated
        /// when AddressSpace is set.
        /// </summary>
        public int InternalBAR
        {
            get;
            private set;
        }

        /// <summary>
        /// IsRecordingSession indicates if this IRegDriver instance is a "recording driver" (a.k.a.
        /// control stream). This is used to add a recording indication in register logs.
        /// </summary>
        public bool IsRecordingSession
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Call Close when IRegDriver will not longer be used, typically just before the Sessions are
        /// closed. For most implementations of IRegDriver this is a NOP, but for some this is important
        /// for cleanup (e.g. MemoryMapRegDriver needs to unmap the memory map -- which is why
        /// IRegDriver.Close() should be called before the Sessions are closed.
        /// </summary>
        public void Close()
        {
            // NOP
        }


        /// <summary>
        /// Set the timeout of the active session to default (2 sec)
        /// </summary>
        public void VisaTimeOutDefault()
        {
            // NOP
        }

        /// <summary>
        /// Set the timeout of the active session to msTimeout
        /// </summary>
        /// <param name="msTimeout">VISA session timeout in milliseconds</param>
        public void VisaTimeOut( int msTimeout )
        {
            // NOP
        }

        /// <summary>
        /// Set the timeout of the specified session (Sessions[(short)bar]) to default (2 sec)
        /// </summary>
        /// <param name="bar">session index</param>
        public void VisaTimeOutDefault( AddressSpace bar )
        {
            // NOP
        }

        /// <summary>
        /// Set the timeout of the specified session  (Sessions[(short)bar]) to msTimeout
        /// </summary>
        /// <param name="bar">session index</param>
        /// <param name="msTimeout">VISA session timeout in milliseconds</param>
        public void VisaTimeOut( AddressSpace bar, int msTimeout )
        {
            // NOP
        }

        public void BeginBuffering()
        {
            throw new NotImplementedException();
        }

        public void EndBuffering()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Writes the contents of a register in the current AddressSpace to hardware.
        /// </summary>
        /// <param name="barOffset">Register's offset within AddressSpace.</param>
        /// <param name="value">32 bit Register value.</param>
        public void RegWrite( int barOffset, int value )
        {
            mHardware[ barOffset ] = value;
            WriteCount++;
            if( WriteNotify != null )
            {
                WriteNotify( this, barOffset, value );
            }
        }

        /// <summary>
        /// Writes the contents of a register in the current AddressSpace to hardware.
        /// </summary>
        /// <param name="barOffset">Register's offset within AddressSpace.</param>
        /// <param name="value">64 bit Register value.</param>
        public void RegWrite( int barOffset, long value )
        {
            mHardware[ barOffset ] = value;
            WriteCount++;
            if( WriteNotify != null )
            {
                WriteNotify( this, barOffset, value );
            }
        }

        /// <summary>
        /// Writes the contents of a register in the specified AddressSpace to hardware.
        /// </summary>
        /// <param name="space">AddressSpace of the register</param>
        /// <param name="barOffset">Register's offset within AddressSpace.</param>
        /// <param name="value">32 bit Register value.</param>
        public void RegWrite( AddressSpace space, int barOffset, int value )
        {
            // TODO CORE: use 'space' ... until then, treat all AddressSpace the same
            RegWrite( barOffset, value );
        }

        /// <summary>
        /// Writes the contents of a register in the specified AddressSpace to hardware.
        /// </summary>
        /// <param name="space">AddressSpace of the register</param>
        /// <param name="barOffset">Register's offset within AddressSpace.</param>
        /// <param name="value">64 bit Register value.</param>
        public void RegWrite( AddressSpace space, int barOffset, long value )
        {
            // TODO CORE: use 'space' ... until then, treat all AddressSpace the same
            RegWrite( barOffset, value );
        }

        /// <summary>
        /// Reads the contents of a register in the current AddressSpace from hardware.
        /// </summary>
        /// <param name="barOffset">Register's offset within AddressSpace.</param>
        /// <returns>register's 32 bit value</returns>
        public int RegRead( int barOffset )
        {
            ReadCount++;
            return ( mHardware.ContainsKey( barOffset ) ) ? (int)mHardware[ barOffset ] : 0;
        }

        /// <summary>
        /// Reads the contents of a register in the current AddressSpace from hardware.
        /// </summary>
        /// <param name="barOffset">Register's offset within AddressSpace.</param>
        /// <returns>register's 64 bit value</returns>
        public long RegRead64( int barOffset )
        {
            ReadCount++;
            return ( mHardware.ContainsKey( barOffset ) ) ? mHardware[ barOffset ] : 0;
        }

        /// <summary>
        /// Reads the contents of a register in the specified AddressSpace from hardware.
        /// </summary>
        /// <param name="space">AddressSpace of the register</param>
        /// <param name="barOffset">Register's offset within AddressSpace.</param>
        /// <returns>register's 32 bit value</returns>
        public int RegRead( AddressSpace space, int barOffset )
        {
            // TODO CORE: use 'space' ... until then, treat all AddressSpace the same
            return RegRead( barOffset );
        }

        /// <summary>
        /// Reads the contents of a register in the specified AddressSpace from hardware.
        /// </summary>
        /// <param name="space">AddressSpace of the register</param>
        /// <param name="barOffset">Register's offset within AddressSpace.</param>
        /// <returns>register's 64 bit value</returns>
        public long RegRead64( AddressSpace space, int barOffset )
        {
            // TODO CORE: use 'space' ... until then, treat all AddressSpace the same
            return RegRead64( barOffset );
        }

        public void RegRefresh( IRegister[] regArray )
        {
            RegRefresh( regArray, 0, regArray.Length - 1 );
        }

        public void RegRefresh( IRegister[] regArray, int startIndex, int endIndex )
        {
            for( int j = startIndex; j <= endIndex; j++ )
            {
                regArray[ j ].UpdateRegVal();
            }
        }

        public void ArrayWrite( int barOffset, byte[] data8 )
        {
            HardwareArray8[ barOffset ] = data8;
        }

        public void ArrayWrite( int barOffset, int[] data32 )
        {
            HardwareArray32[ barOffset ] = data32;
        }

        public void ArrayWrite( int barOffset, int[] data32, int length )
        {
            int[] subset = new int[length];
            Array.Copy( data32, 0, subset, 0, length );
            HardwareArray32[ barOffset ] = subset;
        }

        public void ArrayWrite( int barOffset, int[] data32, int length, int offset )
        {
            int[] subset = new int[length];
            Array.Copy( data32, offset, subset, 0, length );
            HardwareArray32[ barOffset ] = subset;
        }

        public void ArrayWrite( int barOffset, byte[] data8, int startByte, int numBytes )
        {
            byte[] subset = new byte[numBytes];
            Array.Copy( data8, startByte, subset, 0, numBytes );
            HardwareArray8[ barOffset ] = subset;
        }

        public void ArrayWrite( AddressSpace bar, int barOffset, byte[] data8, int startIndex, int numBytes )
        {
            throw new NotImplementedException();
        }

        public void ArrayWrite( AddressSpace bar, int barOffset, int[] data32, int length )
        {
            throw new NotImplementedException();
        }

        public void ArrayWrite( AddressSpace bar, int barOffset, int[] data32, int length, int offset )
        {
            throw new NotImplementedException();
        }

        public void FifoWrite( AddressSpace PxiBar, int[] data32 )
        {
            throw new NotImplementedException();
        }

        public void FifoWrite( AddressSpace PxiBar, int[] data32, int length )
        {
            throw new NotImplementedException();
        }

        public void ArrayRead( int barOffset, ref byte[] data8, int startIndex, int numBytes )
        {
            if( HardwareArray8.ContainsKey( barOffset ) )
            {
                byte[] cache = HardwareArray8[ barOffset ];
                if( cache.Length >= numBytes )
                {
                    Array.Copy( cache, 0, data8, startIndex, numBytes );
                }
                else
                {
                    Array.Copy( cache, 0, data8, startIndex, cache.Length );
                    Array.Clear( data8, startIndex + cache.Length, numBytes - cache.Length );
                }
            }
            else
            {
                Array.Clear( data8, startIndex, numBytes );
            }
        }

        public void ArrayRead( int barOffset, ref int[] data32, int startIndex, int num32BitWords )
        {
            if( HardwareArray32.ContainsKey( barOffset ) )
            {
                int[] cache = HardwareArray32[ barOffset ];
                if( cache.Length >= num32BitWords )
                {
                    Array.Copy( cache, 0, data32, startIndex, num32BitWords );
                }
                else
                {
                    Array.Copy( cache, 0, data32, startIndex, cache.Length );
                    Array.Clear( data32, startIndex + cache.Length, num32BitWords - cache.Length );
                }
            }
            else
            {
                Array.Clear( data32, startIndex, num32BitWords );
            }
        }

        public void ArrayRead( AddressSpace bar, int barOffset, ref byte[] data8, int startIndex, int numBytes )
        {
            throw new NotImplementedException();
        }

        public byte[] ArrayRead8( int barOffset, int numBytes )
        {
            byte[] data8 = new byte[numBytes];
            ArrayRead( barOffset, ref data8, 0, numBytes );
            return data8;
        }

        public byte[] ArrayRead8( AddressSpace bar, int barOffset, int numBytes )
        {
            throw new NotImplementedException();
        }

        public int[] ArrayRead32( int barOffset, int num32BitWords )
        {
            int[] data32 = new int[num32BitWords];
            ArrayRead( barOffset, ref data32, 0, num32BitWords );
            return data32;
        }

        public int[] ArrayRead32( AddressSpace bar, int barOffset, int num32BitWords )
        {
            throw new NotImplementedException();
        }

        public void ReadFifo( AddressSpace bar, int barOffset, int length, int[] data, int offset )
        {
            throw new NotImplementedException();
        }

        public void ReadFifo( AddressSpace bar, int barOffset, int length, int[] data )
        {
            throw new NotImplementedException();
        }

        public void ReadFifo( AddressSpace bar, int barOffset, int length, byte[] data, int offset )
        {
            throw new NotImplementedException();
        }

        public void ReadFifo( AddressSpace bar, int barOffset, int length, byte[] data )
        {
            throw new NotImplementedException();
        }

        #endregion IRegDriver
    }
}