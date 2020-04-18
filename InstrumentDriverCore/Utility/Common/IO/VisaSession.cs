/******************************************************************************
 *                                                                         
 *               Copyright 2011 .
 *               All rights reserved.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace InstrumentDriver.Core.Common.IO
{
    public class VisaSession : ISession
    {
        #region delegates

        /// <summary>
        /// A SlotNumberDelegate determines the slot of the module.  This may by used with property
        /// SlotNumberDelegate to provide an alternate method of determining slot number (the default
        /// is to read the VISA attribute VI_ATTR_SLOT)
        /// </summary>
        /// <returns></returns>
        public delegate short SlotNumberDelegate();

        #endregion

        #region Default Resource Manager variables (singleton)

        private static readonly object mResourceManagerLock = new object();
        private static int mResourceManagerCount;
        private static int mResourceManager;

        #endregion Default Resource Manager variables

        #region Session variables

        private int mSession;
        private short mSlot = -1;
        private short mModelCode = -1;
        private string mModelName;
        private readonly Dictionary <int, long> mSimulatedRegisters = new Dictionary <int, long>();
        private int mDefaultTimeout = 5000; // ms
        private readonly object mVirtualMemoryLock = new object();
        private int mVirtualMemorySize;
        private IntPtr mVirtualMemory = (IntPtr)0;

        private String mViDesc;
        private int mTimeout;

        #endregion

        #region Unmanaged Imports and Definitions

        [Flags]
        public enum AllocationType : uint
        {
// ReSharper disable InconsistentNaming
            COMMIT = 0x1000,
            RESERVE = 0x2000,
            RESET = 0x80000,
            LARGE_PAGES = 0x20000000,
            PHYSICAL = 0x400000,
            TOP_DOWN = 0x100000,
            WRITE_WATCH = 0x200000
// ReSharper restore InconsistentNaming
        }

        [Flags]
        public enum MemoryProtection : uint
        {
// ReSharper disable InconsistentNaming
            EXECUTE = 0x10,
            EXECUTE_READ = 0x20,
            EXECUTE_READWRITE = 0x40,
            EXECUTE_WRITECOPY = 0x80,
            NOACCESS = 0x01,
            READONLY = 0x02,
            READWRITE = 0x04,
            WRITECOPY = 0x08,
            GUARD_Modifierflag = 0x100,
            NOCACHE_Modifierflag = 0x200,
            WRITECOMBINE_Modifierflag = 0x400
// ReSharper restore InconsistentNaming
        }

        [Flags]
        public enum MemoryFreeType : uint
        {
// ReSharper disable InconsistentNaming
            MEM_DECOMMIT = 0x4000,
            MEM_RELEASE = 0x8000
// ReSharper restore InconsistentNaming
        }


        [DllImport( "kernel32.dll", SetLastError = true )]
        public static extern IntPtr VirtualAlloc( IntPtr lpAddress,
                                                  UIntPtr dwSize,
                                                  AllocationType flAllocationType,
                                                  MemoryProtection flProtect );

        [DllImport( "kernel32.dll", SetLastError = true )]
        public static extern bool VirtualFree( IntPtr lpAddress,
                                               UIntPtr dwSize,
                                               uint dwFreeType );

        #endregion Unmanaged Imports and Definitions

        /// <summary>
        /// Construct a VISA session with the default slot number "fallback register" of 0x430
        /// in BAR0 (matches the TLO common carrier hardware).  
        /// NOTE: the "fallback register" is only used if reading VISA attribute VI_ATTR_SLOT fails
        /// </summary>
        public VisaSession()
            : this( AddressSpace.PxiBar0, 0x430 )
        {
        }

        /// <summary>
        /// Construct a VISA session with the specified slot number "fallback register".
        /// NOTE: the "fallback register" is only used if reading VISA attribute VI_ATTR_SLOT fails
        /// </summary>
        /// <param name="slotNumberAddressSpace"></param>
        /// <param name="slotNumberRegisterAddress"></param>
        public VisaSession( AddressSpace slotNumberAddressSpace, short slotNumberRegisterAddress )
        {
            SlotNumberAddressSpace = slotNumberAddressSpace;
            SlotNumberRegisterAddress = slotNumberRegisterAddress;

            lock( mResourceManagerLock )
            {
                if( mResourceManager == 0 )
                {
                    CheckStatus( AgVisa32.viOpenDefaultRM( out mResourceManager ) );
                }
                mResourceManagerCount++;
            }
        }

        private static void CheckStatus( int status )
        {
            // Information > 0,  VI_SUCCESS == 0, Error < 0
            if( status < 0 )
            {
                throw new VisaException( status );
            }
        }

        /// <summary>
        /// Open the underlying driver with an exclusive lock (VISA, Eiger, whatever)
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
        /// 
        /// If you need a shared session (VI_SHARED_LOCK), call Open() with exclusive==false then call LockSession.
        /// </summary>
        /// <param name="resource">the VISA resource descriptor.  Ignored if simulated is true</param>
        /// <param name="timeout">I/O timeout in ms ... not all drivers/sessions use this</param>
        /// <param name="simulated">if true, no connection to hardware will be made</param>
        /// <param name="exclusive">if true opens session exclusively (VI_EXCLUSIVE_LOCK).  if false opens session with no lock (VI_NO_LOCK)</param>
        public void Open( string resource, int timeout, bool simulated, bool exclusive )
        {
            // If already open, report an error...
            if( IsSimulated || mSession != 0 )
            {
                throw new IOException( "VisaSession is already open" );
            }
            
            mViDesc = resource;
            mTimeout = timeout;

            // Cache the resource descriptor -- may be used to open additional sessions to the same resource
            ResourceDescriptor = resource;
            Timeout = timeout;
            mDefaultTimeout = timeout;

            if( simulated )
            {
                // NOTE: if you want control of these values, use MockSession
                IsSimulated = true;
                mSlot = 1;
                mModelCode = 0x1234;
                mModelName = resource;
                mSession = resource.GetHashCode();
                return;
            }

            // Make sure cached values are cleared
            mSlot = -1;
            mModelCode = -1;
            mModelName = string.Empty;

            CheckStatus( AgVisa32.viOpen(
                mResourceManager,
                resource,
                ( exclusive ) ? AgVisa32.VI_EXCLUSIVE_LOCK : AgVisa32.VI_NO_LOCK,
                timeout,
                out mSession ) );

            // Cache the locked status
            IsSessionLocked = exclusive;

            // Use the supplied timeout as the default timeout for I/O operations
            CheckStatus( AgVisa32.viSetAttribute( mSession, AgVisa32.VI_ATTR_TMO_VALUE, timeout ) );

            // NOTE: DMA default settings ... For Agilent IOLS 16.3.16603 these are
            //    VI_ATTR_DMA_ALLOW_EN 0
            //    VI_ATTR_SRC_INCREMENT 1
            //    VI_ATTR_DEST_INCREMENT 1
            //    VI_AGATTR_DMA_WRITE_THRESHOLD 512
            //    VI_AGATTR_DMA_READ_THRESHOLD 48
            //    VI_AGATTR_DMA_CHANNEL -1
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
            // Release any virtual memory...
            lock( mVirtualMemoryLock )
            {
                if( mVirtualMemory != (IntPtr)0 )
                {
                    VirtualFree( mVirtualMemory, (UIntPtr)0, (uint)MemoryFreeType.MEM_RELEASE );
                    mVirtualMemory = (IntPtr)0;
                    mVirtualMemorySize = 0;
                }
            }
        }

        /// <summary>
        /// Close the underlying driver
        /// </summary>
        public void Close()
        {
            if( mSession != 0 )
            {
                CheckStatus( AgVisa32.viClose( mSession ) );
                mSession = 0;
            }
            // Make sure cached values are cleared
            mSlot = -1;
            mModelCode = -1;
            mModelName = string.Empty;
            // Release any virtual memory...
            lock( mVirtualMemoryLock )
            {
                if( mVirtualMemory != (IntPtr)0 )
                {
                    VirtualFree( mVirtualMemory, (UIntPtr)0, (uint)MemoryFreeType.MEM_RELEASE );
                    mVirtualMemory = (IntPtr)0;
                    mVirtualMemorySize = 0;
                }
            }
        }

        public VisaSession NewSession()
        {
            VisaSession newSession = new VisaSession();

            Int32 lockMode;

            AgVisa32.viGetAttribute(mSession, AgVisa32.VI_ATTR_RSRC_LOCK_STATE,
                                    out lockMode);

            if (lockMode != AgVisa32.VI_NO_LOCK)
            {
                // Unlock the old session so it can be shared.
                AgVisa32.viUnlock(mSession);
            }

            // Open a new session with the same resource descriptor
            // and simulation mode as the old session.
            newSession.Open(mViDesc, mTimeout, IsSimulated, false);

            return newSession;
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
        /// Return the underlying VISA session handle/id
        /// </summary>
        public int SessionID
        {
            get
            {
                return mSession;
            }
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

        #region EventRelatedMethods

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
            // If not open, report an error...
            if( mSession == 0 )
            {
                throw new IOException( "Cannot lock a closed VisaSession." );
            }

            const int VI_ERROR_TMO = -1073807339; // = 0xBFFF0015
            const int VI_ERROR_RSRC_LOCKED = -1073807345; // = 0xBFFF000F

            StringBuilder buffer = new StringBuilder( 256 );
            int error = AgVisa32.viLock( mSession,
                                         ( exclusive ) ? AgVisa32.VI_EXCLUSIVE_LOCK : AgVisa32.VI_SHARED_LOCK,
                                         timeout,
                                         key,
                                         buffer );
            if( error < 0 )
            {
                // If someone else has this locked, we get a timeout ... "translate"
                // that error into "resource locked"
                if( error == VI_ERROR_TMO )
                {
                    error = VI_ERROR_RSRC_LOCKED;
                }
                AgVisa32Exception.Throw( error );
            }
            IsSessionLocked = true;
            return buffer.ToString();
        }

        /// <summary>
        /// Unlock the underlying driver.  Driver may have been locked by LockSession() or
        /// if opened exclusively by Open()
        /// </summary>
        public void UnlockSession()
        {
            CheckStatus( AgVisa32.viUnlock( mSession ) );
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
            CheckStatus( AgVisa32.viInstallHandler( mSession, (int)eventType, eventHandler, parm ) );
        }

        public void UninstallHandler( VisaEvents eventType, AgVisa32.viEventHandler eventHandler, int parm )
        {
            CheckStatus( AgVisa32.viUninstallHandler( mSession, (int)eventType, eventHandler, parm ) );
        }

        public void EnableEvent( VisaEvents eventType, EventMechanism mechanism )
        {
            //error = AgVisa32.viEnableEvent(mSession, (short)eventType, (short)mechanism, 0);
            //error = AgVisa32.viEnableEvent(mSession, AgVisa32.VI_EVENT_PXI_INTR, 
            //                                 AgVisa32.VI_HNDLR, 0);
            //error = AgVisa32.viEnableEvent(mSession, AgVisa32.VI_EVENT_PXI_INTR,
            //                                 (short)mechanism, 0);

            CheckStatus( AgVisa32.viEnableEvent( mSession, (int)eventType, (short)mechanism, 0 ) );
        }


        public void DisableEvent( VisaEvents eventType, EventMechanism mechanism )
        {
            CheckStatus( AgVisa32.viDisableEvent( mSession, (int)eventType, (short)mechanism ) );
        }


        public void WaitOnEvent( VisaEvents eventType, int timeout )
        {
            int outeventtype = 0;
            int outeventcontext = 0;

            CheckStatus( AgVisa32.viWaitOnEvent( mSession,
                                                 (int)eventType,
                                                 timeout,
                                                 ref outeventtype,
                                                 ref outeventcontext ) );
        }

        #endregion EventMethods

        #region I/O Methods

        /// <summary>
        /// Get the specified attribute
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public int GetSessionAttribute( int attribute )
        {
            int value;
            CheckStatus( AgVisa32.viGetAttribute( mSession, attribute, out value ) );
            return value;
        }

        /// <summary>
        /// Set the specified attribute
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetSessionAttribute( int attribute, int value )
        {
            CheckStatus( AgVisa32.viSetAttribute( mSession, attribute, value ) );
        }

        /// <summary>
        /// Perform an input
        /// </summary>
        /// <param name="BAR"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public Int32 In32( short BAR, int offset )
        {
            if( IsSimulated )
            {
                // For now, trivial implementation of simulation ... simply echo what was last written
                return mSimulatedRegisters.ContainsKey( offset ) ? (int)mSimulatedRegisters[ offset ] : 0;
            }

            Int32 value;
            CheckStatus( AgVisa32.viIn32( mSession, BAR, offset, out value ) );
            return value;
        }

        /// <summary>
        /// Perform an output
        /// </summary>
        /// <param name="BAR"></param>
        /// <param name="offset"></param>
        /// <param name="value"></param>
        public void Out32( short BAR, int offset, Int32 value )
        {
            if( IsSimulated )
            {
                // For now, trivial implementation of simulation ... simply cache the value
                mSimulatedRegisters[ offset ] = value;
                return;
            }

            CheckStatus( AgVisa32.viOut32( mSession, BAR, offset, value ) );
        }

        public void Out64( short bar, long barOffset, Int64 writeValue )
        {
            int error = AgVisa32.viOut64( mSession, bar, (int)barOffset, writeValue );

            if( error < 0 )
            {
                AgVisa32Exception.Throw( error );
            }
        }


        public void Out32( short bar, long barOffset, Int32 writeValue )
        {
            if( IsSimulated )
            {
                // For now, trivial implementation of simulation ... simply cache the value
                mSimulatedRegisters[ (int)barOffset ] = writeValue;
                return;
            }

            int error = AgVisa32.viOut32( mSession, bar, (int)barOffset, writeValue );

            if( error < 0 )
            {
                AgVisa32Exception.Throw( error );
            }
        }

        public void MoveOut64( short bar, long barOffset, int length, Int64[] dataArray )
        {
            int error = AgVisa32.viMoveOut64( mSession,
                                              bar,
                                              (int)barOffset,
                                              length,
                                              dataArray );

            if( error < 0 )
            {
                AgVisa32Exception.Throw( error );
            }
        }

        public void MoveOut32PageAligned( short bar, long barOffset, int length, Int32[] dataArray, int dataOffset = 0)
        {
            lock( mVirtualMemoryLock )
            {
                if( mVirtualMemorySize < length )
                {
                    // Need more memory ... release the current memory
                    if( mVirtualMemory != (IntPtr)0 )
                    {
                        VirtualFree( mVirtualMemory, (UIntPtr)0, (uint)MemoryFreeType.MEM_RELEASE );
                    }

                    // NOTE: this allocation allocates memory in units of pages. It is
                    // assumed that the pages are evenly divisible by 8 bytes.
                    mVirtualMemory = VirtualAlloc( (IntPtr)null,
                                                   (UIntPtr)( length * sizeof( int ) ),
                                                   AllocationType.COMMIT,
                                                   MemoryProtection.READWRITE );
                    if( mVirtualMemory == (IntPtr)0 )
                    {
                        mVirtualMemorySize = 0;
                        AgVisa32Exception.Throw( AgVisa32.VI_ERROR_ALLOC );
                    }
                    mVirtualMemorySize = length;
                }

                Marshal.Copy(dataArray, dataOffset, mVirtualMemory, length);

                int error = AgVisa32.viMoveOut32( mSession,
                                                  bar,
                                                  (int)barOffset,
                                                  length,
                                                  mVirtualMemory );

                if( error < 0 )
                {
                    AgVisa32Exception.Throw( error );
                }
            }
        }



        public void MoveOut32( short bar, long barOffset, int length, Int32[] dataArray )
        {
            int error = AgVisa32.viMoveOut32( mSession,
                                              bar,
                                              (int)barOffset,
                                              length,
                                              dataArray );

            if( error < 0 )
            {
                AgVisa32Exception.Throw( error );
            }
        }


        public void MoveOut8( short bar, long barOffset, int length, Byte[] dataArray )
        {
            int error = AgVisa32.viMoveOut8( mSession,
                                             bar,
                                             (int)barOffset,
                                             length,
                                             dataArray );

            if( error < 0 )
            {
                AgVisa32Exception.Throw( error );
            }
        }


        public Int32 In32( short bar, long barOffset )
        {
            int readval;
            int error = AgVisa32.viIn32( mSession, bar, (int)barOffset, out readval );

            if( error < 0 )
            {
                AgVisa32Exception.Throw( error );
            }

            return readval;
        }

        public Int64 In64( short bar, long barOffset )
        {
            Int64 readval;
            int error = AgVisa32.viIn64( mSession, bar, (int)barOffset, out readval );

            if( error < 0 )
            {
                AgVisa32Exception.Throw( error );
            }

            return readval;
        }


        public Int64[] MoveIn64( short bar, long barOffset, int numToRead )
        {
            Int64[] buf64 = new Int64[numToRead];
            int error = AgVisa32.viMoveIn64( mSession,
                                             bar,
                                             (int)barOffset,
                                             numToRead,
                                             buf64 );

            if( error < 0 )
            {
                AgVisa32Exception.Throw( error );
            }

            return buf64;
        }

        public Int32[] MoveIn32( short bar, long barOffset, int numToRead )
        {
            Int32[] buf32 = new Int32[numToRead];
            int error = AgVisa32.viMoveIn32( mSession,
                                             bar,
                                             (int)barOffset,
                                             numToRead,
                                             buf32 );
            if( error < 0 )
            {
                AgVisa32Exception.Throw( error );
            }

            return buf32;
        }

        public Int32[] MoveIn32PageAligned( short bar, long barOffset, int numToRead )
        {
            Int32[] inArray;

            lock( mVirtualMemoryLock )
            {
                if( mVirtualMemorySize < numToRead )
                {
                    // Need more memory ... release the current memory
                    if( mVirtualMemory != (IntPtr)0 )
                    {
                        VirtualFree( mVirtualMemory, (UIntPtr)0, (uint)MemoryFreeType.MEM_RELEASE );
                    }

                    // NOTE: this allocation allocates memory in units of pages. It is
                    // assumed that the pages are evenly divisible by 8 bytes.
                    mVirtualMemory = VirtualAlloc( (IntPtr)null,
                                                   (UIntPtr)( numToRead * sizeof( int ) ),
                                                   AllocationType.COMMIT,
                                                   MemoryProtection.READWRITE );
                    if( mVirtualMemory == (IntPtr)0 )
                    {
                        mVirtualMemorySize = 0;
                        AgVisa32Exception.Throw( AgVisa32.VI_ERROR_ALLOC );
                    }
                    mVirtualMemorySize = numToRead;
                }

                int error = AgVisa32.viMoveIn32( mSession,
                                                 bar,
                                                 (int)barOffset,
                                                 numToRead,
                                                 mVirtualMemory );

                if( error < 0 )
                {
                    AgVisa32Exception.Throw( error );
                }

                inArray = new Int32[numToRead];
                Marshal.Copy( mVirtualMemory, inArray, 0, numToRead );
            }

            return inArray;
        }


        public void MoveIn32PageAligned( short bar, long barOffset, int numToRead, Int32[] inData, int offset )
        {
            lock( mVirtualMemoryLock )
            {
                if( mVirtualMemorySize < numToRead )
                {
                    // Need more memory ... release the current memory
                    if( mVirtualMemory != (IntPtr)0 )
                    {
                        VirtualFree( mVirtualMemory, (UIntPtr)0, (uint)MemoryFreeType.MEM_RELEASE );
                    }

                    // NOTE: this allocation allocates memory in units of pages. It is
                    // assumed that the pages are evenly divisible by 8 bytes.
                    mVirtualMemory = VirtualAlloc( (IntPtr)null,
                                                   (UIntPtr)( numToRead * sizeof( int ) ),
                                                   AllocationType.COMMIT,
                                                   MemoryProtection.READWRITE );
                    if( mVirtualMemory == (IntPtr)0 )
                    {
                        mVirtualMemorySize = 0;
                        AgVisa32Exception.Throw( AgVisa32.VI_ERROR_ALLOC );
                    }
                    mVirtualMemorySize = numToRead;
                }

                int error = AgVisa32.viMoveIn32( mSession,
                                                 bar,
                                                 (int)barOffset,
                                                 numToRead,
                                                 mVirtualMemory );

                if( error < 0 )
                {
                    AgVisa32Exception.Throw( error );
                }

                Marshal.Copy( mVirtualMemory, inData, offset, numToRead );
            }
        }

        public void MoveIn32( short bar, long barOffset, Int32[] data, int numToRead )
        {
            int error = AgVisa32.viMoveIn32( mSession,
                                             bar,
                                             (int)barOffset,
                                             numToRead,
                                             data );

            if( error < 0 )
            {
                AgVisa32Exception.Throw( error );
            }
        }

        public byte[] MoveIn8( short bar, long barOffset, int numBytesToRead )
        {
            byte[] buf8 = new byte[numBytesToRead];
            int error = AgVisa32.viMoveIn8( mSession,
                                            bar,
                                            (int)barOffset,
                                            numBytesToRead,
                                            buf8 );

            if( error < 0 )
            {
                AgVisa32Exception.Throw( error );
            }

            return buf8;
        }

        public void MoveIn8PageAligned( short bar, long barOffset, int numBytesToRead, byte[] inData, int offset )
        {
            IntPtr inArrayUnmanaged = (IntPtr)0;

            lock( mVirtualMemoryLock )
            {
                int num32BitWords = numBytesToRead / 4;
                if( ( numBytesToRead & 0x3 ) != 0 )
                {
                    num32BitWords++;
                }

                if( mVirtualMemorySize < num32BitWords )
                {
                    // Need more memory ... release the current memory
                    if( mVirtualMemory != (IntPtr)0 )
                    {
                        VirtualFree( mVirtualMemory, (UIntPtr)0, (uint)MemoryFreeType.MEM_RELEASE );
                    }

                    // NOTE: this allocation allocates memory in units of pages. It is
                    // assumed that the pages are evenly divisible by 8 bytes.
                    mVirtualMemory = VirtualAlloc( (IntPtr)null,
                                                   (UIntPtr)( num32BitWords * sizeof( int ) ),
                                                   AllocationType.COMMIT,
                                                   MemoryProtection.READWRITE );
                    if( mVirtualMemory == (IntPtr)0 )
                    {
                        mVirtualMemorySize = 0;
                        AgVisa32Exception.Throw( AgVisa32.VI_ERROR_ALLOC );
                    }
                    mVirtualMemorySize = num32BitWords;
                }

                int error = AgVisa32.viMoveIn32( mSession,
                                                 bar,
                                                 (int)barOffset,
                                                 num32BitWords,
                                                 inArrayUnmanaged );

                if( error < 0 )
                {
                    AgVisa32Exception.Throw( error );
                }

                Marshal.Copy( inArrayUnmanaged, inData, offset, numBytesToRead );
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
            CheckStatus( AgVisa32.viMapAddress( mSession, mapSpace, mapOffset, mapSize, accMode, suggested, out address ) );
        }

        /// <summary>
        /// This operation unmaps the region previously mapped by the MapAddress() operation.
        /// </summary>
        public void UnmapAddress()
        {
            CheckStatus( AgVisa32.viUnmapAddress( mSession ) );
        }

        /// <summary>
        /// This operation reads an 8-bit value from the address location specified in addr. The address
        /// must be a valid memory address in the current process mapped by a previous MapAddress() call.
        /// </summary>
        /// <param name="addr">Specifies the source address to read the value.</param>
        /// <returns></returns>
        public byte Peek8( IntPtr addr )
        {
            byte value;
            AgVisa32.viPeek8( mSession, addr, out value );
            return value;
        }

        /// <summary>
        /// This operation reads an 16-bit value from the address location specified in addr. The address
        /// must be a valid memory address in the current process mapped by a previous MapAddress() call.
        /// </summary>
        /// <param name="addr">Specifies the source address to read the value.</param>
        /// <returns></returns>
        public short Peek16( IntPtr addr )
        {
            short value;
            AgVisa32.viPeek16( mSession, addr, out value );
            return value;
        }

        /// <summary>
        /// This operation reads an 32-bit value from the address location specified in addr. The address
        /// must be a valid memory address in the current process mapped by a previous MapAddress() call.
        /// </summary>
        /// <param name="addr">Specifies the source address to read the value.</param>
        /// <returns></returns>
        public int Peek32( IntPtr addr )
        {
            int value;
            AgVisa32.viPeek32( mSession, addr, out value );
            return value;
        }

        /// <summary>
        /// This operation reads an 32-bit value from the address location specified in addr. The address
        /// must be a valid memory address in the current process mapped by a previous MapAddress() call.
        /// </summary>
        /// <param name="addr">Specifies the source address to read the value.</param>
        /// <returns></returns>
        public long Peek64( IntPtr addr )
        {
            long value;
            AgVisa32.viPeek64( mSession, addr, out value );
            return value;
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
            AgVisa32.viPoke8( mSession, addr, value );
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
            AgVisa32.viPoke16( mSession, addr, value );
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
            AgVisa32.viPoke32( mSession, addr, value );
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
            AgVisa32.viPoke64( mSession, addr, value );
        }

        #endregion I/O Methods

        #region Specific Attributes

        public string ModelName
        {
            get
            {
                if( string.IsNullOrEmpty( mModelName ) && ! IsSimulated )
                {
                    StringBuilder buffer = new StringBuilder( 64 );
                    CheckStatus( AgVisa32.viGetAttribute( mSession, AgVisa32.VI_ATTR_MODEL_NAME, buffer ) );
                    mModelName = buffer.ToString();
                }
                return mModelName;
            }
        }

        public short ModelCode
        {
            get
            {
                if( mModelCode < 0 && ! IsSimulated )
                {
                    CheckStatus( AgVisa32.viGetAttribute( mSession, AgVisa32.VI_ATTR_MODEL_CODE, out mModelCode ) );
                }
                return mModelCode;
            }
        }

        /// <summary>
        /// SlotNumberMethod defines an optional method used to determine the module's
        /// slot number in lieu of querying the VISA attribute VI_ATTR_SLOT
        /// </summary>
        public SlotNumberDelegate SlotNumberMethod
        {
            get;
            set;
        }

        /// <summary>
        /// SlotNumberAddressSpace and SlotNumberRegisterAddress are used to determine the
        /// modules slot number via a register read if SlotNumberMethod is not define and the
        /// read of VI_ATTR_SLOT fails. Defaults to PxiBar0 (in constructor)
        /// </summary>
        public AddressSpace SlotNumberAddressSpace
        {
            get;
            set;
        }

        /// <summary>
        /// SlotNumberAddressSpace and SlotNumberRegisterAddress are used to determine the
        /// modules slot number via a register read if SlotNumberMethod is not define and the
        /// read of VI_ATTR_SLOT fails.  Defaults to 0x400 (in constructor)
        /// </summary>
        public short SlotNumberRegisterAddress
        {
            get;
            set;
        }

        /// <summary>
        /// Read the slot number of the module. There are severals ways this may be determined:
        /// 1) If SlotNumberMethod is defined, that method will be called
        /// 2) Read the VISA attribute VI_ATTR_SLOT
        /// 3) If #2 fails, read the register specified by SlotNumberAddressSpace and
        ///    SlotNumberRegisterAddress (which can be specified in the constructor or
        ///    set via the corresponding mutators).
        /// </summary>
        public short SlotNumber
        {
            get
            {
                if( mSlot < 0 && ! IsSimulated )
                {
                    // First choice, if a SlotDelegate is defined, use that
                    if( SlotNumberMethod != null )
                    {
                        mSlot = SlotNumberMethod();
                    }
                    else
                    {
                        // Else read the VISA attribute ... VI_ATTR_SLOT
                        CheckStatus( AgVisa32.viGetAttribute( mSession, AgVisa32.VI_ATTR_SLOT, out mSlot ) );
                        if( mSlot == -1 )
                        {
                            // If reading VI_ATTR_SLOT fails, perform a read of a specific register
                            int temp;
                            AgVisa32.viIn32( mSession,
                                             (short)SlotNumberAddressSpace,
                                             SlotNumberRegisterAddress,
                                             out temp );
                            mSlot = (short)( temp & 0x1F );
                        }
                    }
                }
                return mSlot;
            }
        }

        /// <summary>
        /// Enable/disable use of DMA. For VISA implementations this corresponds to VI_ATTR_DMA_ALLOW_EN
        /// </summary>
        public bool DmaEnabled
        {
            get
            {
                int value;
                CheckStatus( AgVisa32.viGetAttribute( mSession, AgVisa32.VI_ATTR_DMA_ALLOW_EN, out value ) );
                return value != 0;
            }
            set
            {
                CheckStatus( AgVisa32.viSetAttribute( mSession, AgVisa32.VI_ATTR_DMA_ALLOW_EN, value ? 1 : 0 ) );
            }
        }

        /// <summary>
        /// Set/get the DMA source increment. For VISA implementations this corresponds to VI_ATTR_SRC_INCREMENT
        /// </summary>
        public int DmaSourceIncrement
        {
            get
            {
                int value;
                CheckStatus( AgVisa32.viGetAttribute( mSession, AgVisa32.VI_ATTR_SRC_INCREMENT, out value ) );
                return value;
            }
            set
            {
                CheckStatus( AgVisa32.viSetAttribute( mSession, AgVisa32.VI_ATTR_SRC_INCREMENT, value ) );
            }
        }

        /// <summary>
        /// Set/get the DMA destination increment. For VISA implementations this corresponds to VI_ATTR_DEST_INCREMENT
        /// </summary>
        public int DmaDestinationIncrement
        {
            get
            {
                int value;
                CheckStatus( AgVisa32.viGetAttribute( mSession, AgVisa32.VI_ATTR_DEST_INCREMENT, out value ) );
                return value;
            }
            set
            {
                CheckStatus( AgVisa32.viSetAttribute( mSession, AgVisa32.VI_ATTR_DEST_INCREMENT, value ) );
            }
        }

        /// <summary>
        /// Set/get the DMA read threshold. For VISA implementations this corresponds to VI_AGATTR_DMA_READ_THRESHOLD
        /// </summary>
        public int DmaReadThreshold
        {
            get
            {
                int value;
                CheckStatus( AgVisa32.viGetAttribute( mSession, AgVisa32.VI_AGATTR_DMA_READ_THRESHOLD, out value ) );
                return value;
            }
            set
            {
                CheckStatus( AgVisa32.viSetAttribute( mSession, AgVisa32.VI_AGATTR_DMA_READ_THRESHOLD, value ) );
            }
        }

        /// <summary>
        /// Set/get the DMA write threshold. For VISA implementations this corresponds to VI_AGATTR_DMA_WRITE_THRESHOLD
        /// </summary>
        public int DmaWriteThreshold
        {
            get
            {
                int value;
                CheckStatus( AgVisa32.viGetAttribute( mSession, AgVisa32.VI_AGATTR_DMA_WRITE_THRESHOLD, out value ) );
                return value;
            }
            set
            {
                CheckStatus( AgVisa32.viSetAttribute( mSession, AgVisa32.VI_AGATTR_DMA_WRITE_THRESHOLD, value ) );
            }
        }

        /// <summary>
        /// Set/get the DMA channel. For VISA implementations this corresponds to VI_AGATTR_DMA_CHANNEL
        /// </summary>
        public int DmaChannel
        {
            get
            {
                int value;
                CheckStatus( AgVisa32.viGetAttribute( mSession, AgVisa32.VI_AGATTR_DMA_CHANNEL, out value ) );
                return value;
            }
            set
            {
                CheckStatus( AgVisa32.viSetAttribute( mSession, AgVisa32.VI_AGATTR_DMA_CHANNEL, value ) );
            }
        }

        #endregion Specific Attributes

        #region Implementation of IDisposable

        public void Dispose()
        {
            // If the user hasn't called Close() we need to
            // close the VISA session without the possibility
            // of throwing an exception (so call viClose directly)
            if( mSession != 0 )
            {
                int status = AgVisa32.viClose( mSession );
                mSession = 0;
                if( status != 0 )
                {
                    Debug.Print( "AgVisa.viClose(session) failed.  Status={0:x}", status );
                }

                // Release any virtual memory...
                lock( mVirtualMemoryLock )
                {
                    if( mVirtualMemory != (IntPtr)0 )
                    {
                        VirtualFree( mVirtualMemory, (UIntPtr)0, (uint)MemoryFreeType.MEM_RELEASE );
                        mVirtualMemory = (IntPtr)0;
                        mVirtualMemorySize = 0;
                    }
                }

                // Release Default Resource manager
                lock( mResourceManagerLock )
                {
                    mResourceManagerCount--;
                    if( mResourceManagerCount <= 0 )
                    {
                        mResourceManagerCount = 0;
                        if( mResourceManager != 0 )
                        {
                            status = AgVisa32.viClose( mResourceManager );
                            mResourceManager = 0;
                            if( status != 0 )
                            {
                                Debug.Print( "AgVisa.viClose(manager) failed.  Status={0:x}", status );
                            }
                        }
                    }
                }
            }
        }

        #endregion Implementation of IDisposable


    }
}