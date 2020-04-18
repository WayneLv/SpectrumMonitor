/******************************************************************************
 *                                                                         
 *               
 *               All rights reserved.
 *
 *****************************************************************************/

using System;
using InstrumentDriver.Core.Utility.Log;

namespace InstrumentDriver.Core.Register
{
    /// <summary>
    /// The base class for all BitFields. 
    /// </summary>
    /// <remarks>
    /// BitField is accessed via Reflection from LoggerFactory ... so do not obfuscate
    /// </remarks>
    [System.Reflection.ObfuscationAttribute( Exclude = true )]
    public abstract class BitField : IBitField
    {
        private readonly string mName;

        protected readonly byte mStartBit;
        protected readonly byte mSizeInBits;

        protected static ILogger mLogger = LogManager.RootLogger;

        /// <summary>
        /// Creates a BitField
        /// </summary>
        /// <param name="name">The long name of the bit field ... assumed to be {register name}:{bitfield name}</param>
        /// <param name="startBit">The first bit of the BitField ... the LSB is 0</param>
        /// <param name="bitFieldSizeInBits"></param>
        /// <param name="regSizeInBits">Size of the register this BitField will be used with ... generally 32 or 64</param>
        protected BitField( string name,
                            int startBit,
                            int bitFieldSizeInBits,
                            int regSizeInBits )
        {
            mName = name;
            mStartBit = (byte)startBit;
            mSizeInBits = (byte)bitFieldSizeInBits;

            if( startBit + bitFieldSizeInBits > regSizeInBits )
            {
                throw new ApplicationException(
                    string.Format( "Register '{0}' startBit ({1}) + bitFieldSizeInBits ({2}) exceeds RegSize ({3})",
                                   name,
                                   startBit,
                                   bitFieldSizeInBits,
                                   regSizeInBits ) );
            }
        }

        protected static long CreateMask( Int32 regSizeInBits, Int32 fieldSizeInBits, Int32 startBit )
        {
            long mask;


            if( fieldSizeInBits == regSizeInBits )
            {
                unchecked
                {
                    mask = (long)0xffffffffffffffff;
                }
            }
            else
            {
                unchecked
                {
                    mask = ~( (long)0xffffffffffffffff << fieldSizeInBits ) << startBit;
                }
            }

            return mask;
        }

        /// <summary>
        /// The full name of the BitField -- normally this is the value specified to the
        /// constructor and is assumed to be in the form of {register name}:{bitfield name}
        /// </summary>
        public string Name
        {
            get
            {
                return mName;
            }
        }

        /// <summary>
        /// The "short name" of the BitField (just the BitField name sans the register name).
        /// Normally this is the portion of the (full) Name following ":".
        /// </summary>
        public string ShortName
        {
            get
            {
                // remove all characters up to and including the ":" (the RegName) in order to
                //  get just the BitField portion of the name.
                int firstCharOfShortName = mName.IndexOf( ":" ) + 1;

                return mName.Substring( firstCharOfShortName ).Trim();
            }
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
        public abstract object Resource
        {
            get;
        }

        /// <summary>
        /// Gets or Sets the cached latch value, does not access HW.  A set leaves the register
        /// marked dirty. Use Apply() to write dirty values to HW or use Write() to "directly"
        /// write value to HW.
        /// </summary>
        public abstract int Value
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or Sets the cached latch value, does not access HW.  A set leaves the register
        /// marked dirty. Use Apply() to write dirty values to HW or use Write() to "directly"
        /// write value to HW.
        /// </summary>
        public abstract long Value64
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the bit field size in bits.
        /// </summary>
        public int Size
        {
            get
            {
                return mSizeInBits;
            }
        }

        /// <summary>
        /// Gets the bit field starting bit position.
        /// </summary>
        public int StartBit
        {
            get
            {
                return mStartBit;
            }
        }

        /// <summary>
        /// Gets the bit field ending bit position.
        /// </summary>
        public int EndBit
        {
            get
            {
                return mStartBit + mSizeInBits - 1;
            }
        }

        /// <summary>
        /// Gets the mask for the bit field within the register.
        /// </summary>
        public abstract long Mask
        {
            get;
        }

        /// <summary>
        /// Writes the value to HW.
        /// </summary>
        /// <param name="value">value to write.</param>
        public abstract void Write( int value );

        /// <summary>
        /// Writes the value to HW.
        /// </summary>
        /// <param name="driver">driver(port in moonstone terms) to write to.</param>
        /// <param name="value">value to write.</param>
        public abstract void Write( IRegDriver driver, int value );

        /// <summary>
        /// Reads BitField value from HW.
        /// </summary>
        public abstract int Read();

        /// <summary>
        /// Writes the value to HW.
        /// </summary>
        public abstract void Write( long value );

        /// <summary>
        /// Reads BitField value from HW.
        /// </summary>
        public abstract long Read64();

        public abstract void Apply( bool forceApply );

        public abstract void Apply( IRegDriver driver, bool forceApply );

        /// <summary>
        /// Return a reference to the register this field belongs to.  THIS MAY BE NULL!!!
        /// </summary>
        public abstract IRegister Register
        {
            get;
        }

        #region Register bit locking methods

        /// <summary>
        /// Prevent register bits from being modified.
        /// </summary>
        public abstract void LockBits();

        /// <summary>
        /// Allow register bits to be modified.
        /// </summary>
        public abstract void UnlockBits();

        #endregion Register bit locking methods

    }
}