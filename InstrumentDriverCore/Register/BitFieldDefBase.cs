/******************************************************************************
 *                                                                         
 *               
 *               All rights reserved.
 *
 *****************************************************************************/

using System;

namespace InstrumentDriver.Core.Register
{
    public class BitFieldDefBase
    {
        /// <summary>
        /// Multi purpose variable, serves as index into an array of BitField
        /// enums, so it must start at 0 and sequence with no gaps.
        /// Also is the BitField name.  NOTE: the enum Type for this enum
        /// must be the one described in RegDef.BFType.
        /// </summary>
        public int nameEnum;

        public int width;
        public int startBit;

        public BitFieldDefBase( int bitFieldEnum, int startBit, int endBit, object args )
        {
            if( startBit > endBit )
            {
                throw new ApplicationException( "endBit must be greater than or equal to startBit" );
            }
            this.nameEnum = bitFieldEnum;
            width = endBit - startBit + 1;
            this.startBit = startBit;
            Args = args;
        }

        public BitFieldDefBase( int bitFieldEnum, int startBit, int endBit ) :
            this( bitFieldEnum, startBit, endBit, null )
        {
        }

        public BitFieldDefBase( int bitFieldEnum, int bit ) :
            this( bitFieldEnum, bit, bit, null )
        {
        }

        public object Args
        {
            get;
            set;
        }

        /// <summary>
        /// Int value of the Enum of the register that contains this bitfield.
        /// This is used as an index into an array of registers so at to identify
        /// which register this BitField is part of. 
        /// </summary>
        public int RegEnum
        {
            get;
            protected set;
        }

        /// <summary>
        /// This is the BitField type to match to registers.  The intent is if BFType
        /// is non-null it overrides 'regEnum' and this definition applies to any
        /// register that has IRegister.BFType == BFType. 
        /// </summary>
        public Type BFType
        {
            get;
            set;
        }

        /// <summary>
        /// Optional.  If set, Condition is evaluated by RegFactory.Evaluate and if
        /// the expression is true the BitField is created/included. If the expression
        /// is false then the BitField is not created.
        /// </summary>
        /// <remarks>
        /// As of 16-Sept-2015 Condition takes the form of {Key}{op}{Value} where
        /// 
        /// Key ..... string value passed to IInstrument.Service.GetValue to retrieve the value of interest
        /// op ...... normal comparision operators (==, !=, etc.) or 'match'/'nomatch' which performs Regex.IsMatch( GetValue(Key), Value )
        /// Value ... the comparison value.
        /// 
        /// All of the comparison operators will convert the operands to integer values 
        /// </remarks>
        public string Condition
        {
            get;
            set;
        }
    }
}