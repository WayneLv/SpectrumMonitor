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
    /// Defines a register.
    /// </summary>
    public class RegDef
    {
        private const int DEFAULT_REG_SIZE = 32; // in bits

        /// <summary>
        /// Multi purpose variable. Serves as index into the created Reg array, so it must
        /// start at 0 and sequence with no gaps. Also is the Register name
        /// </summary>
        public int nameEnum;

        /// <summary>
        /// BARoffset is an offset from "something". Typically this is an offset into a memory
        /// block for PCIe BAR (Base Address Register) -- i.e. a register.  Different types of
        /// registers may use this as an offset to an arbitrary concept (e.g. "device registers"
        /// use this as the index of a device's internal registers).
        /// </summary>
        public int BARoffset;

        /// <summary>
        /// Enum type that defines the BitFields within the register. This enum type must:
        /// start at 0, be sequential in value with no gaps, and have an enum value for
        /// each and every BitField within the register.
        /// </summary>
        public Type BFenum;

        public RegType RegType;

        /// <summary>
        /// The suggested/preferred implementation for this register (due to the history of
        /// development, many factory methods ignore this field and the field may be null)
        /// </summary>
        public Type RegImplementationType;

        /// <summary>
        /// NEW: Most register types ignore this!
        /// 
        /// The size of the register in bits (8,16,24,32...).  Typically used to control
        /// the number of byte operations that make up a single read/write for
        /// device registers.
        /// </summary>
        public int Size
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

        public RegDef( int nameEnumValue, int barOffset, Type bfEnumType )
            :this( nameEnumValue, barOffset, bfEnumType, RegType.RW, DEFAULT_REG_SIZE, null, string.Empty )
        {
        }

        public RegDef( int nameEnumValue, int barOffset, Type bfEnumType, RegType eRegType )
            :this( nameEnumValue, barOffset, bfEnumType, eRegType, DEFAULT_REG_SIZE, null, string.Empty )
        {
        }

        public RegDef( int nameEnumValue, int barOffset, Type bfEnumType, RegType eRegType, int size )
            :this( nameEnumValue, barOffset, bfEnumType, eRegType, size, null, string.Empty )
        {
        }

        public RegDef( int nameEnumValue, int barOffset, Type bfEnumType, RegType eRegType, int size, string condition )
            :this( nameEnumValue, barOffset, bfEnumType, eRegType, size, null, condition )
        {
        }

        public RegDef( int nameEnumValue, int barOffset, Type bfEnumType, RegType eRegType, string condition )
            :this( nameEnumValue, barOffset, bfEnumType, eRegType, DEFAULT_REG_SIZE, null, condition )
        {
        }

        public RegDef( int nameEnumValue, int barOffset, Type bfEnumType, RegType eRegType, Type regImplementationType )
            :this( nameEnumValue, barOffset, bfEnumType, eRegType, DEFAULT_REG_SIZE, regImplementationType, string.Empty )
        {
        }

        public RegDef( int nameEnumValue, int barOffset, Type bfEnumType, RegType eRegType, Type regImplementationType, string condition )
            :this( nameEnumValue, barOffset, bfEnumType, eRegType, DEFAULT_REG_SIZE, regImplementationType, condition )
        {
        }

        public RegDef( int nameEnumValue, int barOffset, Type bfEnumType, RegType eRegType, int size, Type regImplementationType, string condition )
        {
            nameEnum = nameEnumValue;
            BARoffset = barOffset;
            BFenum = bfEnumType;
            RegType = eRegType;
            RegImplementationType = regImplementationType;
            Condition = condition;
            Size = size; // Most register types ignore this ...
        }
    }
}