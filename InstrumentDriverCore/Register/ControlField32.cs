/******************************************************************************
 *                                                                         
 *               
 *               .
 *
 *****************************************************************************/

using InstrumentDriver.Core.Utility.Log;

namespace InstrumentDriver.Core.Register
{
    /// <summary>
    /// ControlReg is a specialization of RegField32 aimed at "Control" registers
    /// registers.  The difference is ControlReg ALWAYS READS back from hardware on writes.  This is to avoid 
    /// Shadow register collisions caused by modular core use.
    /// </summary>
    public class ControlField32 : RegField32
    {
        public ControlField32(string bitFieldName,
                               int startBit,
                               int sizeInBits,
                               IRegister reg)
            : base(bitFieldName, startBit, sizeInBits, reg)
        {
        }

        /// <summary>
        /// Reads back to refresh the shadow register before write.
        /// The shadow register can be in an incorrect state when using the control stream
        /// </summary>
        /// <param name="value"></param>
        public override void Write(int value)
        {
            //Refresh the shadow register to avoid modular core/control stream collisions
            mRegister.Read32();

            base.Write(value);
        }

       
    }
}