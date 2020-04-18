/******************************************************************************
 *
 *               
 *               All rights reserved.
 *
 *****************************************************************************/

using System;
using System.Threading;
using InstrumentDriver.Core.Common.IO;

namespace InstrumentDriver.Core.Register
{
    public interface IRegDriver
    {
        #region Synchronization

        /// <summary>
        /// The synchronization object used to control access to IRegDriver.
        /// NOTE: As of 25-Oct-2012 this is ineffective ... it was copied from APeX implementation
        ///       which only accessed the Mutex in very few locations (which effectively means it
        ///       does NOT ensure exclusive access...
        /// </summary>
        Mutex Resource
        {
            get;
        }

        #endregion

        #region Session(s)

        /// <summary>
        /// The ISession(s) objects used for I/O.  Sessions[0] is the session
        /// for AgVisa32.VI_PXI_BAR0_SPACE, Sessions[1] is the session for
        /// AgVisa32.VI_PXI_BAR1_SPACE, etc.
        ///       
        /// </summary>
        ISession[] Sessions
        {
            get;
            set;
        }

        /// <summary>
        /// Return the ISession object for the specified BAR (Base Address Register).
        ///       
        /// </summary>
        /// <param name="bar"></param>
        /// <returns></returns>
        ISession Session( AddressSpace bar );

        /// <summary>
        /// Return the ISession object corresponding to the current value of AddressSpace
        /// This is the same as:   return Session(AddressSpace);
        ///       
        /// </summary>
        ISession ActiveSession
        {
            get;
        }

        /// <summary>
        /// Get/set the address space (e.g. BAR0, BAR1, ...) that I/O operations
        /// will use.  This also selects which ISession object is used.  E.g.
        ///    Session( AddressSpace ).Out32( (short)AddressSpace, BARoffset, value );
        /// 
        /// </summary>
        AddressSpace AddressSpace
        {
            get;
            set;
        }

        /// <summary>
        /// InternalBAR is the "internal" VISA representation of VI_ATTR_PXI_MEM_BASE_BARx (i.e. the
        /// value returned by GetSessionAttribute( AgVisa32.VI_ATTR_PXI_MEM_BASE_BARx ) for a specific
        /// session).  This value is used ONLY by control streams / peer-2-peer forwarding.  All
        /// VISA calls should use the VISA enumeration and NOT this property.  This value is updated
        /// when AddressSpace is set.
        /// </summary>
        int InternalBAR
        {
            get;
        }

        /// <summary>
        /// IsRecordingSession indicates if this IRegDriver instance is a "recording driver" (a.k.a.
        /// control stream). This is used to add a recording indication in register logs.
        /// </summary>
        bool IsRecordingSession
        {
            get;
        }

        /// <summary>
        /// Call Close when IRegDriver will not longer be used, typically just before the Sessions are
        /// closed. For most implementations of IRegDriver this is a NOP, but for some this is important
        /// for cleanup (e.g. MemoryMapRegDriver needs to unmap the memory map -- which is why
        /// IRegDriver.Close() should be called before the Sessions are closed.
        /// </summary>
        void Close();

        #endregion Session(s)

        #region Timeout

        /// <summary>
        /// Set the timeout of the active session to default (2 sec)
        /// </summary>
        void VisaTimeOutDefault();

        /// <summary>
        /// Set the timeout of the active session to msTimeout
        /// </summary>
        /// <param name="msTimeout">VISA session timeout in milliseconds</param>
        void VisaTimeOut( Int32 msTimeout );

        /// <summary>
        /// Set the timeout of the specified session (Sessions[(short)bar]) to default (2 sec)
        /// </summary>
        /// <param name="bar">session index</param>
        void VisaTimeOutDefault( AddressSpace bar );

        /// <summary>
        /// Set the timeout of the specified session  (Sessions[(short)bar]) to msTimeout
        /// </summary>
        /// <param name="bar">session index</param>
        /// <param name="msTimeout">VISA session timeout in milliseconds</param>
        void VisaTimeOut( AddressSpace bar, Int32 msTimeout );

        #endregion Timeout

        #region  Reg Access Methods

        void BeginBuffering();
        void EndBuffering();

        /// <summary>
        /// Writes the contents of a register in the current AddressSpace to hardware.
        /// </summary>
        /// <param name="barOffset">Register's offset within AddressSpace.</param>
        /// <param name="value">32 bit Register value.</param>
        void RegWrite( int barOffset, int value );

        /// <summary>
        /// Writes the contents of a register in the current AddressSpace to hardware.
        /// </summary>
        /// <param name="barOffset">Register's offset within AddressSpace.</param>
        /// <param name="value">64 bit Register value.</param>
        void RegWrite( int barOffset, long value );

        /// <summary>
        /// Writes the contents of a register in the specified AddressSpace to hardware.
        /// </summary>
        /// <param name="space">AddressSpace of the register</param>
        /// <param name="barOffset">Register's offset within AddressSpace.</param>
        /// <param name="value">32 bit Register value.</param>
        void RegWrite( AddressSpace space, int barOffset, int value );

        /// <summary>
        /// Writes the contents of a register in the specified AddressSpace to hardware.
        /// </summary>
        /// <param name="space">AddressSpace of the register</param>
        /// <param name="barOffset">Register's offset within AddressSpace.</param>
        /// <param name="value">64 bit Register value.</param>
        void RegWrite( AddressSpace space, int barOffset, long value );

        /// <summary>
        /// Reads the contents of a register in the current AddressSpace from hardware.
        /// </summary>
        /// <param name="barOffset">Register's offset within AddressSpace.</param>
        /// <returns>register's 32 bit value</returns>
        int RegRead( int barOffset );

        /// <summary>
        /// Reads the contents of a register in the current AddressSpace from hardware.
        /// </summary>
        /// <param name="barOffset">Register's offset within AddressSpace.</param>
        /// <returns>register's 64 bit value</returns>
        long RegRead64( int barOffset );

        /// <summary>
        /// Reads the contents of a register in the specified AddressSpace from hardware.
        /// </summary>
        /// <param name="space">AddressSpace of the register</param>
        /// <param name="barOffset">Register's offset within AddressSpace.</param>
        /// <returns>register's 32 bit value</returns>
        int RegRead( AddressSpace space, int barOffset );

        /// <summary>
        /// Reads the contents of a register in the specified AddressSpace from hardware.
        /// </summary>
        /// <param name="space">AddressSpace of the register</param>
        /// <param name="barOffset">Register's offset within AddressSpace.</param>
        /// <returns>register's 64 bit value</returns>
        long RegRead64( AddressSpace space, int barOffset );

        #endregion

        #region  RegApply and RegRefresh methods

        ///// <summary>
        ///// Writes a registers value to hardware if dirty.
        ///// </summary>
        ///// <param name="regArray">Array of registers to write to hardware.</param>
        ///// <param name="startIndex">Index of first register to Apply.</param>
        ///// <param name="endIndex">Index of last register to Apply.</param>
        ///// <param name="ForceApply">Write reg contents even if not dirty.</param>
        // void RegApply(RegBase[] regArray, int startIndex, int endIndex,
        //                        bool ForceApply);

        ///// <summary>
        ///// Writes a registers value to hardware if dirty.
        ///// </summary>
        ///// <param name="regArray">Array of registers to write to hardware.</param>
        ///// <param name="startIndex">Index of first register to Apply.</param>
        ///// <param name="endIndex">Index of last register to Apply.</param>
        ///// <param name="ForceApply">Write reg contents even if not dirty.</param>
        // void RegApply(IRegDriver driver, RegBase[] regArray, int startIndex, int endIndex,
        //                        bool ForceApply);
        /// <summary>
        /// Update the register value from hardware of an array of registers.
        /// </summary>
        void RegRefresh( IRegister[] regArray );

        /// <summary>
        /// Update the register value from hardware of an array of registers.
        /// </summary>
        void RegRefresh( IRegister[] regArray, int startIndex, int endIndex );

        #endregion

        #region  Array Access methods

        void ArrayWrite( int barOffset, byte[] data8 );
        void ArrayWrite( int barOffset, Int32[] data32 );
        void ArrayWrite( int barOffset, Int32[] data32, int length );
        void ArrayWrite( int barOffset, Int32[] data32, int length, int offset );
        void ArrayWrite( int barOffset, byte[] data8, Int32 startByte, Int32 numBytes );
        void ArrayWrite( AddressSpace bar, int barOffset, byte[] data8, int startIndex, int numBytes );
        void ArrayWrite( AddressSpace bar, int barOffset, int[] data32, int length );
        void ArrayWrite( AddressSpace bar, int barOffset, int[] data32, int length, int offset );
        void FifoWrite( AddressSpace PxiBar, int[] data32 );
        void FifoWrite( AddressSpace PxiBar, int[] data32, int length );

        void ArrayRead( int barOffset, ref byte[] data8, Int32 startIndex, Int32 numBytes );
        void ArrayRead( int barOffset, ref Int32[] data32, Int32 startIndex, Int32 num32BitWords );
        void ArrayRead( AddressSpace bar, int barOffset, ref byte[] data8, Int32 startIndex, Int32 numBytes );
        void ReadFifo( AddressSpace bar, int barOffset, int length, int[] data );
        void ReadFifo( AddressSpace bar, int barOffset, int length, int[] data, int offset );
        void ReadFifo( AddressSpace bar, int barOffset, int length, byte[] data );
        void ReadFifo( AddressSpace bar, int barOffset, int length, byte[] data, int offset );

        // int WaitForEvent(int eventMask, int msTimeout, WaitHandle abortEvent);
        ///// <summary>
        ///// Inserts delay between command execution
        ///// </summary>
        ///// <param name="microseconds">Number of microseconds of delay</param>
        ///// <remarks>
        ///// Use the Wait method to insert a delay between device accesses.
        ///// The LocalPort class implementation of Wait does a busy wait for wait
        ///// times less than 1 milliseconds and uses System.Thread.Sleep for 
        ///// longer wait times.
        ///// </remarks>
        // void Wait(int microseconds);

        byte[] ArrayRead8( int barOffset, Int32 numBytes );
        byte[] ArrayRead8( AddressSpace bar, int barOffset, int numBytes );
        Int32[] ArrayRead32( int barOffset, Int32 num32BitWords );
        int[] ArrayRead32( AddressSpace bar, int barOffset, int num32BitWords );

        #endregion
    }
}