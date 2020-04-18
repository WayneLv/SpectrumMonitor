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
    /// BitFieldDef is a struct that uniquely defines a BitField from every other
    /// BitField.  Its used to create arrays of BitFieldDefs that are parsed
    /// by the BitFieldCreator to create BitFields that are used to 
    /// modify bits in hardware registers, thereby controlling hardware behavior. 
    /// Arrays of BitFieldDef's provide a quick and simple way to create BitFields
    /// with very minimal coding.  Therefore they provide an easy way to create and maintain
    /// BitFields.
    /// </summary>
    public class BitFieldDef : BitFieldDefBase
    {
        public BitFieldDef( int regEnum, int bitFieldEnum, int startBit, int endBit, object args, string condition ) :
            base( bitFieldEnum, startBit, endBit, args )
        {
            RegEnum = regEnum;
            BFType = null;
            Condition = condition;
        }

        public BitFieldDef( int regEnum, int bitFieldEnum, int startBit, int endBit, object args ) :
            this( regEnum, bitFieldEnum, startBit, endBit, args, string.Empty )
        {
        }

        public BitFieldDef( int regEnum, int bitFieldEnum, int startBit, int endBit ) :
            this( regEnum, bitFieldEnum, startBit, endBit, null, string.Empty )
        {
        }

        public BitFieldDef( int regEnum, int bitFieldEnum, int startBit, int endBit, string condition ) :
            this( regEnum, bitFieldEnum, startBit, endBit, null, condition )
        {
        }

        public BitFieldDef( int regEnum, int bitFieldEnum, int bit ) :
            this( regEnum, bitFieldEnum, bit, bit, null, string.Empty )
        {
        }

        public BitFieldDef( int regEnum, int bitFieldEnum, int bit, string condition ) :
            this( regEnum, bitFieldEnum, bit, bit, null, condition )
        {
        }

        public BitFieldDef( Type bitFieldType, int bitFieldEnum, int bit, object args, string condition ) :
            base( bitFieldEnum, bit, bit, args )
        {
            BFType = bitFieldType;
            Condition = condition;
        }

        public BitFieldDef( Type bitFieldType, int bitFieldEnum, int startBit, int endBit, object args, string condition ) :
            base( bitFieldEnum, startBit, endBit, args )
        {
            BFType = bitFieldType;
            Condition = condition;
        }

        public BitFieldDef( Type bitFieldType, int bitFieldEnum, int startBit, int endBit ) :
            base( bitFieldEnum, startBit, endBit, null )
        {
            BFType = bitFieldType;
        }

        public BitFieldDef( Type bitFieldType, int bitFieldEnum, int bit ) :
            this( bitFieldType, bitFieldEnum, bit, bit )
        {
        }
    }
}