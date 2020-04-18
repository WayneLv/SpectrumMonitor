/******************************************************************************
 *                                                                         
 *               
 *               All rights reserved.
 *
 *****************************************************************************/

using System;

namespace InstrumentDriver.Core.Register
{
    /// <summary>
    /// The initial role of this class is only to allow a set of
    /// registers to behave as a single group on the Write or the
    /// Apply.  In other words, if any of the registers in the group
    /// are written or need to be applied, all the registers in the 
    /// group will be written or applied as one set.
    /// </summary>
    public class RegSet
    {
        protected int mIndexLastReg;
        protected int mIndexFirstReg;
        protected Reg <Int32>[] mRegArray;
        private bool mRegSetDirty;

        public RegSet( IRegister[] regArray ) : this( regArray, 0, regArray.Length - 1 )
        {
        }

        public RegSet( IRegister[] regArray, int indexFirstReg, int indexLastReg )
        {
            // TODO CORE: refactor RegSet (and IRegister?) to work without explicit Reg<Int32>
            mRegArray = new Reg <Int32>[regArray.Length];
            mIndexFirstReg = indexFirstReg;
            mIndexLastReg = indexLastReg;

            for( int i = indexFirstReg; i <= indexLastReg; i++ )
            {
                mRegArray[ i ] = regArray[ i ] as Reg <Int32>;
                mRegArray[ i ].PutRegInRegSet( this );
            }
        }

        /// <summary>
        /// Create a RegSet that associates the register in the specified array.  When
        /// any register needs to be written, they are all written in the order the
        /// registers appear in the array
        /// </summary>
        /// <remarks>
        /// The RegSet instance is maintained by the registers 
        /// </remarks>
        /// <param name="regArray"></param>
        public static void CreateRegSet( IRegister[] regArray )
        {
            // NOTE: RegSet "installs" the instance in each of the registers, so nothing
            //       else needs to keep track of the instance
// ReSharper disable UnusedVariable
            var instance = new RegSet( regArray, 0, regArray.Length - 1 );
// ReSharper restore UnusedVariable
        }

        /// <summary>
        /// Create a RegSet that associates the register in the specified array.  When
        /// any register needs to be written, they are all written in the order the
        /// registers appear in the array
        /// </summary>
        /// <param name="regArray"></param>
        /// <param name="indexFirstReg"></param>
        /// <param name="indexLastReg"></param>
        public static void CreateRegSet( IRegister[] regArray, int indexFirstReg, int indexLastReg )
        {
            // NOTE: RegSet "installs" the instance in each of the registers, so nothing
            //       else needs to keep track of the instance
// ReSharper disable UnusedVariable
            var instance = new RegSet( regArray, indexFirstReg, indexLastReg );
// ReSharper restore UnusedVariable
        }

        /// <summary>
        /// Number of registers.
        /// </summary>
        public int Size
        {
            get
            {
                return mIndexLastReg - mIndexFirstReg + 1;
            }
        }

        public bool NeedApply
        {
            set
            {
                mRegSetDirty = value;
            }
            get
            {
                return mRegSetDirty;
            }
        }

        internal void DriverWrite()
        {
            for( int i = mIndexFirstReg; i <= mIndexLastReg; i++ )
            {
                mRegArray[ i ].DriverWrite();
            }

            mRegSetDirty = false; // handled in individual DriverWrite cmds?
        }

        public void DriverWrite( IRegDriver driver )
        {
            for( int i = mIndexFirstReg; i <= mIndexLastReg; i++ )
            {
                mRegArray[ i ].DriverWrite( driver );
            }
            mRegSetDirty = false; // handled in individual DriverWrite cmds?
        }

        /// <summary>
        /// Writes an array of registers values.
        /// </summary>
        /// <param name="registerValues">The array of values to write. 
        /// Always starts writing from index 0 to either max size of array
        /// or max number or registers in the set.</param>
        public void Write( int[] registerValues )
        {
            int numWrites = registerValues.Length;
            if( numWrites > Size )
            {
                numWrites = Size;
            }

            for( int i = 0; i < numWrites; i++ )
            {
                mRegArray[ i + mIndexFirstReg ].Value = registerValues[ i ];
            }

            DriverWrite(); // write out the new values regardless of whether they are dirty or not.
        }

        public void Apply( bool force )
        {
            if( NeedApply || force )
            {
                DriverWrite(); // writes out all value regardless if dirty or not.
            }
        }


        public Reg <Int32> this[ int index ]
        {
            get
            {
                return mRegArray[ index + mIndexFirstReg ];
            }
        }

        //internal void Read(int[] registerArray)
        //{
        //    for (int i = mIndexFirstReg; i <= mIndexLastReg; i++)
        //        mRegArray[i].DriverRead();

        //    if (registerArray != null)
        //        throw new Exception("Not Implemented");

        //   // mRegSetDirty = false; // handled in individual DriverRead cmds
        //}

        public int ReadRegister( int offset )
        {
            if( offset >= Size )
            {
                throw new Exception( "Index out of range." );
            }
            return mRegArray[ mIndexFirstReg + offset ].Read();
        }

        public void WriteRegister( IRegDriver activeDriver, int offset, int value )
        {
            if( offset >= Size )
            {
                throw new Exception( "Index out of range." );
            }
            mRegArray[ mIndexFirstReg + offset ].Value = value;
            mRegArray[ mIndexFirstReg + offset ].Apply( activeDriver, true );
        }


        /// <summary>
        /// 
        /// </summary>
        public void UpdateFromHardware()
        {
            for( int i = mIndexFirstReg; i <= mIndexLastReg; i++ )
            {
                mRegArray[ i ].DriverRead();
            }
        }
    }
}