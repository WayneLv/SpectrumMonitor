/******************************************************************************
 *                                                                         
 *               Copyright 2011 .
 *               All rights reserved.
 *
 *****************************************************************************/

using System;

namespace InstrumentDriver.Core.Common.IO
{
    public interface ISession : IDisposable
    {
        /// <summary>
        /// Return the underlying session ID ... the interpretation/use of the ID
        /// depends on the specific instantiation
        /// </summary>
        int SessionID
        {
            get;
        }

        /// <summary>
        /// Returns the resource descriptor used by Open().   Only valid after Open()
        /// has been called.
        /// </summary>
        string ResourceDescriptor
        {
            get;
        }

        /// <summary>
        /// Open the underlying driver with an exclusive lock (VISA, Eiger, whatever). The
        /// default timeout of the session is set to timeout (milliseconds).
        /// </summary>
        /// <param name="resource">the VISA resource descriptor.  Ignored if simulated is true</param>
        /// <param name="timeout">I/O timeout in ms ... not all drivers/sessions use this</param>
        /// <param name="simulated">if true, no connection to hardware will be made</param>
        void Open( string resource, int timeout, bool simulated );

        /// <summary>
        /// Open the underlying driver (VISA, Eiger, whatever). The
        /// default timeout of the session is set to timeout (milliseconds).
        /// 
        /// If you need a shared session (VI_SHARED_LOCK), either
        /// 1) call Open() with exclusive==false then call LockSession.
        /// 2) call Open() with exclusive==true, then call UnlockSession, then call LockSession
        /// </summary>
        /// <param name="resource">the VISA resource descriptor.  Ignored if simulated is true</param>
        /// <param name="timeout">I/O timeout in ms ... not all drivers/sessions use this</param>
        /// <param name="simulated">if true, no connection to hardware will be made</param>
        /// <param name="exclusive">if true opens session exclusively (VI_EXCLUSIVE_LOCK).  if false opens session with no lock (VI_NO_LOCK)</param>
        void Open( string resource, int timeout, bool simulated, bool exclusive );

        /// <summary>
        /// Reset any "volatile" settings to a default state.  Typical settings affected
        /// by this are
        /// * timeout
        /// * cached memory buffers
        /// </summary>
        void Reset();

        /// <summary>
        /// Close the underlying driver
        /// </summary>
        void Close();

        /// <summary>
        /// The current simulation state
        /// </summary>
        bool IsSimulated
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
        string LockSession( bool exclusive, int timeout, string key );

        /// <summary>
        /// Unlock the underlying driver.  Driver may have been locked by LockSession() or
        /// if opened exclusively by Open()
        /// </summary>
        void UnlockSession();

        /// <summary>
        /// Return the lock status of the session.  This does not distinguish between
        /// shared and exclusive locks.  Most implementations simply set a flag in
        /// LockSession or Open and clear it in UnlockSession (i.e. the implementation
        /// may not query the underlying driver)
        /// </summary>
        bool IsSessionLocked
        {
            get;
        }

        /// <summary>
        /// The default timeout of the session. I/O operations that do not explicitly
        /// specify a timeout will use this value.  Initially set by the timeout
        /// parameter used in Open()
        /// </summary>
        int Timeout
        {
            get;
            set;
        }

        #region EventRelatedMethods

        void InstallHandler( VisaEvents eventType, AgVisa32.viEventHandler eventHandler, int parm );

        void UninstallHandler( VisaEvents eventType, AgVisa32.viEventHandler eventHandler, int parm );

        void EnableEvent( VisaEvents eventType, EventMechanism mechanism );

        void DisableEvent( VisaEvents eventType, EventMechanism mechanism );

        void WaitOnEvent( VisaEvents eventType, int timeout );

        #endregion EventMethods

        #region I/O Methods

        /// <summary>
        /// Get the specified attribute
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        int GetSessionAttribute( int attribute );

        /// <summary>
        /// Set the specified attribute
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        void SetSessionAttribute( int attribute, int value );

        /// <summary>
        /// Perform an input
        /// </summary>
        /// <param name="BAR"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        Int32 In32( short BAR, int offset );

        /// <summary>
        /// Perform an output
        /// </summary>
        /// <param name="BAR"></param>
        /// <param name="offset"></param>
        /// <param name="value"></param>
        void Out32( short BAR, int offset, Int32 value );

        void Out64( short bar, long barOffset, Int64 writeValue );

        void MoveOut64( short bar, long barOffset, int length, Int64[] dataArray );

        void MoveOut32PageAligned( short bar, long barOffset, int length, Int32[] dataArray, int dataOffset = 0 );

        void MoveOut32( short bar, long barOffset, int length, Int32[] dataArray );

        void MoveOut8( short bar, long barOffset, int length, Byte[] dataArray );

        Int64 In64( short bar, long barOffset );

        Int64[] MoveIn64( short bar, long barOffset, int numToRead );

        Int32[] MoveIn32( short bar, long barOffset, int numToRead );

        Int32[] MoveIn32PageAligned( short bar, long barOffset, int numToRead );

        void MoveIn32PageAligned( short bar, long barOffset, int numToRead, Int32[] inData, int offset );

        void MoveIn32( short bar, long barOffset, Int32[] data, int numToRead );

        byte[] MoveIn8( short bar, long barOffset, int numBytesToRead );

        void MoveIn8PageAligned( short bar, long barOffset, int numToRead, byte[] inData, int offset );

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
        void MapAddress( short mapSpace, int mapOffset, int mapSize, short accMode, IntPtr suggested, out IntPtr address );

        /// <summary>
        /// This operation unmaps the region previously mapped by the MapAddress() operation.
        /// </summary>
        void UnmapAddress();

        /// <summary>
        /// This operation reads an 8-bit value from the address location specified in addr. The address
        /// must be a valid memory address in the current process mapped by a previous MapAddress() call.
        /// </summary>
        /// <param name="addr">Specifies the source address to read the value.</param>
        /// <returns></returns>
        byte Peek8( IntPtr addr );

        /// <summary>
        /// This operation reads an 16-bit value from the address location specified in addr. The address
        /// must be a valid memory address in the current process mapped by a previous MapAddress() call.
        /// </summary>
        /// <param name="addr">Specifies the source address to read the value.</param>
        /// <returns></returns>
        short Peek16( IntPtr addr );

        /// <summary>
        /// This operation reads an 32-bit value from the address location specified in addr. The address
        /// must be a valid memory address in the current process mapped by a previous MapAddress() call.
        /// </summary>
        /// <param name="addr">Specifies the source address to read the value.</param>
        /// <returns></returns>
        int Peek32( IntPtr addr );

        /// <summary>
        /// This operation reads an 32-bit value from the address location specified in addr. The address
        /// must be a valid memory address in the current process mapped by a previous MapAddress() call.
        /// </summary>
        /// <param name="addr">Specifies the source address to read the value.</param>
        /// <returns></returns>
        long Peek64( IntPtr addr );

        /// <summary>
        /// This operation takes an 8-bit value and stores its content to the address location specified
        /// in addr. The address must be a valid memory address in the current process mapped by a previous
        /// MapAddress() call.
        /// </summary>
        /// <param name="addr">Specifies the destination address to store the value.</param>
        /// <param name="value">data to write</param>
        void Poke8( IntPtr addr, byte value );

        /// <summary>
        /// This operation takes an 16-bit value and stores its content to the address location specified
        /// in addr. The address must be a valid memory address in the current process mapped by a previous
        /// MapAddress() call.
        /// </summary>
        /// <param name="addr">Specifies the destination address to store the value.</param>
        /// <param name="value">data to write</param>
        void Poke16( IntPtr addr, short value );

        /// <summary>
        /// This operation takes an 32-bit value and stores its content to the address location specified
        /// in addr. The address must be a valid memory address in the current process mapped by a previous
        /// MapAddress() call.
        /// </summary>
        /// <param name="addr">Specifies the destination address to store the value.</param>
        /// <param name="value">data to write</param>
        void Poke32( IntPtr addr, int value );

        /// <summary>
        /// This operation takes an 64-bit value and stores its content to the address location specified
        /// in addr. The address must be a valid memory address in the current process mapped by a previous
        /// MapAddress() call.
        /// </summary>
        /// <param name="addr">Specifies the destination address to store the value.</param>
        /// <param name="value">data to write</param>
        void Poke64( IntPtr addr, long value );

        #endregion I/O Methods

        #region Specific Attributes

        string ModelName
        {
            get;
        }

        short ModelCode
        {
            get;
        }

        short SlotNumber
        {
            get;
        }

        /// <summary>
        /// Enable/disable use of DMA. For VISA implementations this corresponds to VI_ATTR_DMA_ALLOW_EN
        /// </summary>
        bool DmaEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// Set/get the DMA source increment. For VISA implementations this corresponds to VI_ATTR_SRC_INCREMENT
        /// </summary>
        int DmaSourceIncrement
        {
            get;
            set;
        }

        /// <summary>
        /// Set/get the DMA destination increment. For VISA implementations this corresponds to VI_ATTR_DEST_INCREMENT
        /// </summary>
        int DmaDestinationIncrement
        {
            get;
            set;
        }

        /// <summary>
        /// Set/get the DMA read threshold. For VISA implementations this corresponds to VI_AGATTR_DMA_READ_THRESHOLD
        /// </summary>
        int DmaReadThreshold
        {
            get;
            set;
        }

        /// <summary>
        /// Set/get the DMA write threshold. For VISA implementations this corresponds to VI_AGATTR_DMA_WRITE_THRESHOLD
        /// </summary>
        int DmaWriteThreshold
        {
            get;
            set;
        }

        /// <summary>
        /// Set/get the DMA channel. For VISA implementations this corresponds to VI_AGATTR_DMA_CHANNEL
        /// </summary>
        int DmaChannel
        {
            get;
            set;
        }

        #endregion Specific Attributes
    }
}