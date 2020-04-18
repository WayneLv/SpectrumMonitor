/******************************************************************************
 *                                                                         
 *               
 *               All rights reserved.
 *
 *****************************************************************************/

using System;
using System.Collections;

namespace InstrumentDriver.Core.Register
{
    /// <summary>
    /// This 'enumerable' class is required for the AddTreePanelReg methods so that it can be
    /// passed a generic type that is independent of the actual Reg32T type.
    /// </summary>
    public class Reg32Enumerable : IEnumerator, IEnumerable
    {
        protected RegBase mReg;
//        uint[] indexList;  //this array holds each enum value from the enum specifying BitFields
//        private int m_IEnumerableIndex;

        public Reg32Enumerable( RegBase r, Type enumType ) : this( r, enumType, false )
        {
        }

        public Reg32Enumerable( RegBase r, Type enumType, Boolean bRelaxedTypeMatching )
        {
            if( r != null )
            {
                // int i = 0;
                mReg = r;

                if( bRelaxedTypeMatching )
                {
                    // in relaxed Type Matching, we only care that n elements in both
                    // enums have the same position and the same name, where n is the
                    // number of enum values in the passed in enumType that overlap enum 
                    // values in the register.
                    Array enumValues = Enum.GetValues( enumType );
                    //if (enumValues.Length >= reg.NumBitFields)
                    //{
                    //    throw new Exception(
                    //        "Relaxed type matching ONLY makes sense if the new enum is < the registers actual enum.");
                    //}

                    // now we get each enum name at the identical position and make sure there is
                    // an exact name match.

                    //reg.Reset();
                    //IEnumerator e = reg.GetEnumerator();


                    //foreach (object enumValue in enumValues)
                    //{
                    int last = (int)enumValues.GetValue( enumValues.Length - 1 );
                    for( int i = mReg.FirstBF; i <= last; i++ )
                    {
                        int enumValue = (int)enumValues.GetValue( i );

                        string relaxedName = Enum.GetName( enumType, enumValue );
                        string bitFieldName = Enum.GetName( mReg.BFType, enumValue );

                        if( String.Compare( relaxedName, bitFieldName ) != 0 )
                        {
                            throw new Exception(
                                string.Format(
                                    "Enum ({0}) names don't match in special relaxed case (obfuscation issue?). '{1}' <> '{2}'",
                                    enumType,
                                    relaxedName,
                                    bitFieldName ) );
                        }
                    }
                }
                else
                {
                    // normal type matching requires an exact match or an exception is thrown.
                    if( !mReg.BFType.Equals( enumType ) )
                    {
                        string s = "Reg enum '" + mReg.BFType.Name + "' does not match ";
                        s += "T type enum '" + enumType.Name + "'";
                        s += "Reg32<T> constructor error: ";
                        throw new ApplicationException( s );
                    }
                }


                ////          indexList = new uint[Enum.GetNames(enumType).Length];
                //          // fill the array with the enum values from the BitField enum.
                //          foreach (int index in Enum.GetValues(enumType))
                //          {
                //             if (i != index)
                //                throw new Exception("indexes don't match.");
                //             indexList[i++] = (uint)index;

                //          }
                Reset(); // reset IEnumerable index
            }
        }

        // IEnumerator requires this
        public IEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            return mReg != null && mReg.MoveNext();
        }

        // IEnumerable require these
        public void Reset()
        {
            if( mReg != null )
            {
                mReg.Reset();
            }
        }

        public object Current
        {
            get
            {
                return mReg == null ? null : mReg.Current;
            }
        }

        public string Name
        {
            get
            {
                return mReg.Name;
            }
        }

        /// <summary> Returns the register value contained in the register class instance. </summary>
        public int Value
        {
            get
            {
                return mReg.Value32;
            }
            set
            {
                mReg.Value32 = value;
            }
        }


        public int Read()
        {
            return mReg.Read32();
        }


        public RegBase GetReg
        {
            get
            {
                return mReg;
            }
        }

        /// <summary>
        /// Reg32 returns the encapsulated RegBase as Reg32.  If the encapsulated
        /// register is not a Reg32 then the return value is null.  The type casting
        /// makes this relatively slow so in general please use GetReg instead.
        /// </summary>
        public Reg32 Reg32
        {
            get
            {
                return mReg as Reg32;
            }
        }

        public virtual void Write( int registerValue )
        {
            mReg.Write32( registerValue );
        }

        public virtual void Write( IRegDriver driver, int registerValue )
        {
            mReg.Write32( driver, registerValue );
        }

        public void Apply( bool forceApply )
        {
            mReg.Apply( forceApply );
        }

        public void Apply( IRegDriver driver, bool forceApply )
        {
            mReg.Apply( driver, forceApply );
        }

        public Boolean NeedApply
        {
            get
            {
                return mReg.NeedApply;
            }
        }

        public Boolean IsApplyEnabled
        {
            get
            {
                return mReg.IsApplyEnabled;
            }
            set
            {
                mReg.IsApplyEnabled = value;
            }
        }
    }

    /// <summary>
    /// Provides intellisense access of a Reg32's BitFields.
    /// </summary>
    /// <typeparam name="TBitFieldEnum">The BitField enum type which specifies the names of the
    /// BitFields you can access in this Register.</typeparam>
    /// <remarks> A wrapper class that allows access to the BitFields of a Reg32 using
    /// an enumerated list of BitField names.  </remarks>
    public class Reg32T < TBitFieldEnum > : Reg32Enumerable
    {
        public Reg32T( RegBase r )
            : base( r, typeof( TBitFieldEnum ) )
        {
        }

        public Reg32T( RegBase r, Boolean bRelaxedTypeChecking )
            : base( r, typeof( TBitFieldEnum ), bRelaxedTypeChecking )
        {
        }

        /// <summary>
        /// Returns a reference to the BitField specified by enumIndex.  This method is faster
        /// to execute than Field( TBitFieldEnum enumIndex) because it doesn't have to do a
        /// Convert.ToUInt32( object ) on the enum value.
        /// </summary>
        /// <param name="enumIndex"></param>
        /// <returns></returns>
        public IBitField Field( uint enumIndex )
        {
            if( mReg == null )
            {
                return null;
            }
            // Revisit, this next if statement is only a short term fix:
            // as a quick fix way to work with the digitizer, which does not implement
            // all common bits fields as originally planned, we allow the common register version
            // of Reg32T enum to contain bitfields not implemented in the register.
            // This is not a real problem because it won't show up in any user interface
            // or module control app, and it is only an internal issue with how common registers 
            // are used by API coders.
            if( enumIndex < mReg.FirstBF )
            {
                string s = String.Format( "BitField {0} is not valid for Register {1}.",
                                          enumIndex,
                                          mReg.Name );
                throw new Exception( s );
            }

            return mReg.GetField( enumIndex );
        }

        /// <summary>
        /// Returns a reference to the BitField specified by enumIndex
        /// </summary>
        /// <param name="enumIndex"></param>
        /// <returns></returns>
        public IBitField Field( TBitFieldEnum enumIndex )
        {
            if( mReg == null )
            {
                return null;
            }
            uint i = Convert.ToUInt32( enumIndex );

            // Revisit, this next if statement is only a short term fix:
            // as a quick fix way to work with the digitizer, which does not implement
            // all common bits fields as originally planned, we allow the common register version
            // of Reg32T enum to contain bitfields not implemented in the register.
            // This is not a real problem because it won't show up in any user interface
            // or module control app, and it is only an internal issue with how common registers 
            // are used by API coders.
            if( i < mReg.FirstBF )
            {
                string s = String.Format( "BitField {0} is not valid for Register {1}.",
                                          enumIndex,
                                          mReg.Name );
                throw new Exception( s );
            }

            return mReg.GetField( i );
        }


        /// <summary>
        /// Returns a reference to the BitField specified by enumIndex
        /// </summary>
        /// <param name="enumIndex"></param>
        /// <returns></returns>
        public IBitField this[TBitFieldEnum enumIndex]
        {
            get
            {
                return Field(enumIndex);
            }
        }    

    }

    // end of class Reg32T
}