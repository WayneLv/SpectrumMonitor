namespace InstrumentDriver.Core.Utility
{
    /// <summary>
    /// IDirtyBit is a simple encapsulation of a boolean, IsDirty, to allow multiple
    /// objects to share a dirty bit.  Typically this is used by registers to keep
    /// track of which groups of registers are dirty.
    /// </summary>
    public interface IDirtyBit
    {
        bool IsDirty
        {
            get;
            set;
        }
    }

    /// <summary>
    /// DirtyBit is a simple encapsulation of a boolean, IsDirty, to allow multiple
    /// objects to share a dirty bit.  Typically this is used by registers to keep
    /// track of which groups of registers are dirty.
    /// </summary>
    public class DirtyBit : IDirtyBit
    {
        public DirtyBit()
        {
            IsDirty = false;
        }

        public DirtyBit( bool isDirty )
        {
            IsDirty = isDirty;
        }

        public bool IsDirty
        {
            get;
            set;
        }
    }
}
