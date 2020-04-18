/******************************************************************************
 *                                                                         
 *               
 *               All rights reserved.
 *
 *****************************************************************************/

namespace InstrumentDriver.Core.Register
{
    /// <summary>
    /// Interface for all BitFields
    /// </summary>
    public interface IBitField
    {
        /// <summary>
        /// The full name of the BitField -- normally this is the value specified to the
        /// constructor and is assumed to be in the form of {register name}:{bitfield name}
        /// </summary>
        string Name
        {
            get;
        }

        /// <summary>
        /// The "short name" of the BitField (just the BitField name sans the register name).
        /// Normally this is the portion of the (full) Name following ":".
        /// </summary>
        string ShortName
        {
            get;
        }

        /// <summary>
        /// The 'resource' the implementing register will lock before performing operations.  If a client
        /// needs to perform multi-field/register operations atomically, execute the code inside:
        /// 
        /// <code>
        ///    lock( IBitField.Resource )
        ///    {
        ///       // atomic code goes here
        ///    }
        /// </code>
        /// </summary>
        /// <remarks>
        /// Normally this is the IRegister.Resource value of the register that 'owns' this IBitField
        /// instance, e.g.   get { return mRegister.Resource; }
        /// </remarks>
        object Resource
        {
            get;
        }

        /// <summary>
        /// Gets or Sets the cached latch value, does not access HW.  A set leaves the register
        /// marked dirty. Use Apply() to write dirty values to HW or use Write() to "directly"
        /// write value to HW.
        /// 
        /// Most implementations preserve all bits outside the current bitfield definition,
        /// but some implementations (CommandField32, CommandField64) clear all other bits.
        /// </summary>
        int Value
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or Sets the cached latch value, does not access HW.  A set leaves the register
        /// marked dirty. Use Apply() to write dirty values to HW or use Write() to "directly"
        /// write value to HW.
        /// 
        /// Most implementations preserve all bits outside the current bitfield definition,
        /// but some implementations (CommandField32, CommandField64) clear all other bits.
        /// </summary>
        long Value64
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the bit field size in bits.
        /// </summary>
        int Size
        {
            get;
        }

        /// <summary>
        /// Gets the bit field starting bit position.
        /// </summary>
        int StartBit
        {
            get;
        }

        /// <summary>
        /// Gets the bit field ending bit position.
        /// </summary>
        int EndBit
        {
            get;
        }

        /// <summary>
        /// Gets the mask for the bit field within the register.
        /// </summary>
        long Mask
        {
            get;
        }

        /// <summary>
        /// Writes the value to HW.
        /// 
        /// Most implementations preserve all bits outside the current bitfield definition,
        /// but some implementations (CommandField32, CommandField64) clear all other bits.
        /// </summary>
        /// <param name="value">value to write.</param>
        void Write( int value );

        /// <summary>
        /// Writes the value to HW.
        /// 
        /// Most implementations preserve all bits outside the current bitfield definition,
        /// but some implementations (CommandField32, CommandField64) clear all other bits.
        /// </summary>
        /// <param name="driver">driver(port in moonstone terms) to write to.</param>
        /// <param name="value">value to write.</param>
        void Write( IRegDriver driver, int value );

        /// <summary>
        /// Reads BitField value from HW.
        /// </summary>
        int Read();

        /// <summary>
        /// Writes the value to HW.
        /// 
        /// Most implementations preserve all bits outside the current bitfield definition,
        /// but some implementations (CommandField32, CommandField64) clear all other bits.
        /// </summary>
        /// <param name="value">value to write.</param>
        void Write( long value );

        /// <summary>
        /// Reads BitField value from HW.
        /// </summary>
        long Read64();

        /// <summary>
        /// Call Apply() of the associated register.  This conditionally writes the register's
        /// value (if dirty) to hardware.
        /// </summary>
        /// <param name="forceApply"></param>
        void Apply( bool forceApply );

        /// <summary>
        /// Call Apply() of the associated register.  This conditionally writes the register's
        /// value (if dirty) to hardware using the specified driver.
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="forceApply"></param>
        void Apply( IRegDriver driver, bool forceApply );

        /// <summary>
        /// Return a reference to the register this field belongs to.  THIS MAY BE NULL!!!
        /// </summary>
        IRegister Register
        {
            get;
        }

        #region Register bit locking methods

        /// <summary>
        /// Prevent register bits from being modified.
        /// </summary>
        void LockBits();

        /// <summary>
        /// Allow register bits to be modified.
        /// </summary>
        void UnlockBits();

        #endregion Register bit locking methods

    }
}