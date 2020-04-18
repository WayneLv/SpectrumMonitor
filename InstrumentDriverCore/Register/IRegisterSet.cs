using InstrumentDriver.Core.Utility;

namespace InstrumentDriver.Core.Register
{
    /// <summary>
    /// RegisterSet implementations are a design pattern within Core to represent
    /// a set of registers that naturally group together.  For instance it may be
    /// desirable to group a set of registers together that reside on a plug-in or
    /// slug, or if the registers have some special meaning as a group.
    /// 
    /// RegisterSet's are managed in the RegManager.   
    /// <see cref="RegManager.AddGroup( string , IRegisterSet )"/>
    /// </summary>
    public interface IRegisterSet
    {
        /// <summary>
        /// Return a vector of the IRegisters that make up the RegisterSet.  Typically
        /// this is created during construction of the class that implements this
        /// interface.
        /// </summary>
        IRegister[] Registers
        {
            get;
        }

        /// <summary>
        /// Get the DirtyBit for the RegisterSet.  Since all Grouped registers share
        /// the same GroupDirtyBit, in most circumstances simply returning the GroupDirtyBit
        /// of the first entry in the Registers is sufficient.
        /// </summary>
        IDirtyBit GroupDirtyBit
        {
            get;
        }

        /// <summary>
        /// Set the initial values of registers.  Each implementation decide what, if any,
        /// register values need to be set and/or written.  The default implementation
        /// is a NOP.
        /// </summary>
        void SetInitialValues();
    }
}