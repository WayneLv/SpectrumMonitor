using System;
using InstrumentDriver.Core.Utility;

namespace InstrumentDriver.Core.Register
{
    /// <summary>
    /// Public interface for register operations.  Not all methods/properties must be implemented
    /// for a given implementation (e.g. Buffer32 does not implement Value32) 
    /// </summary>
    public interface IRegister
    {
        /// <summary>
        /// The IRegDriver this register will use when Apply() is called.  A different IRegDriver may
        /// by used if Apply( IRegDriver, ...) is called.
        /// </summary>
        IRegDriver Driver
        {
            get;
        }

        /// <summary>
        /// The 'resource' the register will lock before performing operations.  If a client needs to perform
        /// multi-register operations atomically, execute the code inside:
        /// 
        /// <code>
        ///    lock( IRegister.Resource )
        ///    {
        ///       // atomic code goes here
        ///    }
        /// </code>
        /// 
        /// *** IMPORTANT *** Setting this property should only be done during construction of the system when
        /// no register operations are in progress.  The register constructor will normally create its own
        /// resource, but to simplify locking it is possible to override this by assigning groups of registers
        /// the same resource - namely, when any register in the group sharing the resource is locked they are
        /// all locked.
        /// </summary>
        object Resource
        {
            get;
            set;
        }

        /// <summary>
        /// Complete name of the register.   Some implementations prepend the module name and identifier
        /// so the register can be identified in a multi-module system.
        /// </summary>
        string Name
        {
            get;
        }

        /// <summary>
        /// The base name of the register (i.e. the register name not qualified by the module name and
        /// identifier)
        /// </summary>
        string NameBase
        {
            get;
        }

        /// <summary>
        /// RegType indicates various attributes of the register such as read-only or volatile.  RegType
        /// is decorated with [Flags], so multiple attributes can be applied (e.g. read-only and volatile)
        /// </summary>
        RegType RegType
        {
            get;
        }

        /// <summary>
        /// If IsMemoryMapped==true, this register reads/writes directly to some memory
        /// location (as opposed to interacting with other registers or serial devices).
        /// The main use for this distinction is for refreshing blocks of registers by
        /// performing a single buffer read (see MemoryMappedControlRegDriver.RegRefresh for
        /// an example coding).
        /// </summary>
        bool IsMemoryMapped
        {
            get;
        }

        /// <summary>
        /// The interpretation of Offset depends on the type of register...
        /// 
        /// For memory mapped registers, Offset is the offset from the BAR (Base Address Register) and
        /// is one of the VISA I/O arguments  (e.g.  viOut( BAR, Offset, Data ))
        /// 
        /// For simple device registers, Offset is usually the address of the register within the device.
        /// </summary>
        Int32 Offset
        {
            get;
        }

        /// <summary>
        /// The size, in bytes, of the register data.
        /// </summary>
        int SizeInBytes
        {
            get;
        }

        /// <summary>
        /// Set or get the software copy of the registers value.
        /// No hardware access will take place.
        /// If this is called on a 64 bit register, the implementation may
        /// either throw an exception or
        ///   get: truncate the value to 32 bits.
        ///   set: zero pad the value to 64 bits.
        /// </summary>
        int Value32
        {
            get;
            set;
        }

        /// <summary>
        /// Set or get the software copy of the registers value.
        /// No hardware access will take place.
        /// If this is called on a 32 bit register, the implementation may
        /// either throw an exception or
        ///   get: zero pad the value to 64 bits.
        ///   set: truncate the value to 32 bits.
        /// </summary>
        long Value64
        {
            get;
            set;
        }

        /// <summary>
        /// Reads a Register value from HW. 
        /// The software copy of the registers value is also updated.
        /// If this is called on a 64 bit register, the implementation may
        /// either throw an exception or truncate the value to 32 bits.
        /// </summary>
        /// <returns></returns>
        int Read32();

        /// <summary>
        /// Reads a Register value from HW. 
        /// The software copy of the registers value is also updated.
        /// If this is called on a 32 bit register, the implementation may
        /// either throw an exception or zero pad the value to 64 bits.
        /// </summary>
        /// <returns></returns>
        long Read64();

        /// <summary>
        /// Writes a register value to HW.  The software copy of the registers value is updated.
        /// If this is called on a 64 bit register, the implementation may
        /// either throw an exception or zero pad the value to 64 bits.
        /// </summary>
        /// <param name="RegisterValue">the value to write to HW></param>
        void Write32( int RegisterValue );

        /// <summary>
        /// Writes a register value to HW using the specified IRegDriver.
        /// The software copy of the registers value is also updated.
        /// If this is called on a 64 bit register, the implementation may
        /// either throw an exception or zero pad the value to 64 bits.
        /// </summary>
        /// <param name="Driver"></param>
        /// <param name="RegisterValue">the value to write to HW></param>
        void Write32( IRegDriver Driver, int RegisterValue );

        /// <summary>
        /// Writes a register value to HW.  The software copy of the registers value is updated.
        /// If this is called on a 32 bit register, the implementation may
        /// either throw an exception or truncate the value to 32 bits.
        /// </summary>
        /// <param name="RegisterValue">the value to write to HW></param>
        void Write64( long RegisterValue );

        /// <summary>
        /// Writes a register value to HW using the specified IRegDriver.
        /// The software copy of the registers value is also updated.
        /// If this is called on a 32 bit register, the implementation may
        /// either throw an exception or truncate the value to 32 bits.
        /// </summary>
        /// <param name="Driver"></param>
        /// <param name="RegisterValue">the value to write to HW></param>
        void Write64( IRegDriver Driver, long RegisterValue );

        /// <summary>
        /// Write Register contents to hardware if dirty (out of sync with Hardware).
        /// </summary>
        /// <param name="ForceApply">Write contents even if not dirty.</param>
        void Apply( bool ForceApply );

        /// <summary>
        /// Write register contents to hardware if dirty (out of sync with Hardware)
        /// using the specified IRegDriver.
        /// </summary>
        /// <param name="Driver"></param>
        /// <param name="ForceApply"></param>
        void Apply( IRegDriver Driver, bool ForceApply );

        /// <summary>
        /// Enable/disable Apply() operation.  When false, Apply() will have no effect
        /// even if force==true.  The main intent of this method is to allow tuning of
        /// register write sequence.  E.g.
        ///       reg1.IsApplyEnabled = false; // prevent the normal sequence from applying the register
        ///       group.ApplyCommonCarrierReg(...);
        ///       ...
        ///       reg1.IsApplyEnabled = true;  // reenable apply and apply it
        ///       reg1.Apply( driver, force );
        /// </summary>
        bool IsApplyEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// Does this register require an Apply? (i.e. is it "dirty")
        /// </summary>
        /// <remarks>
        /// The client code should rarely need to set this value -- the internal
        /// management of dirty should be sufficient for all but exceptional cases.
        /// </remarks>
        bool NeedApply
        {
            get;
            set;
        }

        /// <summary>
        /// Synchronize (i.e. read) the hardware to update the cached register value.
        /// If there is a pending/dirty value (ie. Apply hasn't been called) the
        /// pending value is lost and the dirty flag is cleared.
        /// </summary>
        void UpdateRegVal();

        /// <summary>
        /// Synchronize the cached register value to the supplied value.
        /// If there is a pending/dirty value (ie. Apply hasn't been called) the
        /// pending value is lost and the dirty flag is cleared.
        /// 
        /// A typical use of this method is if a block/buffer read of memory has been
        /// performed (for speed) and the registers corresponding to the block need
        /// to be updated to match the read.
        /// </summary>
        /// <param name="value"></param>
        void UpdateRegVal32( int value );

        /// <summary>
        /// Synchronize the cached register value to the supplied value.
        /// If there is a pending/dirty value (ie. Apply hasn't been called) the
        /// pending value is lost and the dirty flag is cleared.
        /// 
        /// A typical use of this method is if a block/buffer read of memory has been
        /// performed (for speed) and the registers corresponding to the block need
        /// to be updated to match the read.
        /// </summary>
        /// <param name="value"></param>
        void UpdateRegVal64( long value );

        /// <summary>
        /// GroupDirtyBit is an optional way of tracking if any register in a group of registers
        /// is dirty (i.e. Apply() will write the value).  In the default implementation, RegBase,
        /// if NeedApply is set to true, GroupDirtyBit is set to true.  RegBase will never set
        /// GroupDirtyBit to false (that is intended as a application layer operation).
        /// 
        /// NOTE: a register aggregates a single instance of IDirtyBit so if a register is a member
        ///       of multiple groups it will only set the dirty bit for one of them!
        /// </summary>
        IDirtyBit GroupDirtyBit
        {
            get;
            set;
        }

        /// <summary>
        /// "Push" the current cached register state (normally the cached register value and the dirty
        /// flag).  Default implementation is a "stack depth" of 1 (i.e. multiple pushes have no effect).
        /// 
        /// This is intended to work in conjunction with "recording" implementations of IRegDriver (e.g.
        /// IRecordingControl) -- prior to recording an operation the client calls "Push", enables recording,
        /// then performs various register operations (which normally do NOT update the hardware), then
        /// calls "Pop" so the cached register state matches the hardware...
        /// 
        /// This does NOT address module/object properties (normally implementations of ICoreSettings) --
        /// the recording process typically changes property values and calling Apply ultimately clears
        /// any pending change flags.
        /// </summary>
        void Push();

        /// <summary>
        /// "Pop" the saved register state (normally the cached register value and the dirty flag).
        /// Default implementation is a "stack depth" of 1 (i.e. multiple pops have no effect).
        /// 
        /// This is intended to work in conjunction with "recording" implementations of IRegDriver (e.g.
        /// IRecordingControl) -- prior to recording an operation the client calls "Push", enables recording,
        /// then performs various register operations (which normally do NOT update the hardware), then
        /// calls "Pop" so the cached register state matches the hardware...
        /// 
        /// This does NOT address module/object properties (normally implementations of ICoreSettings) --
        /// the recording process typically changes property values and calling Apply ultimately clears
        /// any pending change flags.
        /// </summary>
        void Pop();

        #region Register bit locking methods

        /// <summary>
        /// Prevent register bits from being modified.
        /// </summary>
        /// <param name="lockMask">The desired mask of locked bits></param>
        void LockBits32(Int32 lockMask);

        /// <summary>
        /// Prevent register bits from being modified.
        /// </summary>
        /// <param name="lockMask">The desired mask of locked bits></param>
        void LockBits64(Int64 lockMask);

        /// <summary>
        /// Prevent register bits from being modified.
        /// </summary>
        /// <param name="startBit">Starting bit to lock></param>
        /// <param name="bitCount">Number of bits to lock></param>
        void LockBits(int startBit, int bitCount);

        /// <summary>
        /// Prevent a register from being modified.
        /// </summary>
        void LockBits();

        /// <summary>
        /// Allow register bits to be modified.
        /// </summary>
        /// <param name="startBit">Starting bit to unlock></param>
        /// <param name="bitCount">Number of bits to unlock></param>
        void UnlockBits(int startBit, int bitCount);

        /// <summary>
        /// Allow a register to be modified.
        /// </summary>
        void UnlockBits();

        /// <summary>
        /// Query a register's lock mask.
        /// </summary>
        /// <returns></returns>
        Int32 GetLockMask32();

        /// <summary>
        /// Query a register's lock mask.
        /// </summary>
        /// <returns></returns>
        Int64 GetLockMask64();

        #endregion Register bit locking methods

        #region BitField support

        /// <summary>
        /// Returns the type (normally an Enum) of the bit field identifiers.  If this register
        /// does not support fields, returns null
        /// </summary>
        Type BFType
        {
            get;
        }

        /// <summary>
        /// Return the index of the first BitField.  -1 if there are no BitFields defined.
        /// 
        /// BitFields are internally stored in an array.  "empty" slots contain a null and "non-empty" slots
        /// contain a BitField.  FirstBF is the index of the first non-empty slot.
        /// </summary>
        int FirstBF
        {
            get;
        }

        /// <summary>
        /// Return the index of the last BitField. -1 if there are no BitFields defined.
        /// 
        /// BitFields are internally stored in an array.  "empty" slots contain a null and "non-empty" slots
        /// contain a BitField.  LastBF is the index of the last non-empty slot.
        /// </summary>
        int LastBF
        {
            get;
        }

        /// <summary>
        /// Return the number of BitFields
        /// 
        /// BitFields are internally stored in an array.  "empty" slots contain a null and "non-empty" slots
        /// contain a BitField.  NumBitFields return (LastBF-FirstBF-1) but it is possible for some of the
        /// entries between these values to be null.
        /// </summary>
        int NumBitFields
        {
            get;
        }

        /// <summary>
        /// Returns a reference to the BitField specified by index i.
        /// 
        /// Some implementations also provide one of the following methods (not part of this interface)
        ///     RegField32 Field( uint );
        ///     RegField64 Field( uint );
        /// </summary>
        IBitField GetField( uint i );

        /// <summary>
        /// Adds a Bit Field to a Register
        /// </summary>
        /// <param name="bf">The BitField being added to the register</param>
        /// <remarks>The BitField passed in must be an implementation of the Type.BFType
        /// passed in on construction of the register.  If not, an exception is thrown.</remarks>
        void AddField( IBitField bf );

        /// <summary>
        /// For diagnostic purposes -- normally only called in debug/development
        /// 
        /// Iterate over the BitField array and if there are any gaps (i.e. undefined fields)
        /// display a debug message.
        /// 
        /// This may happen if either
        /// 1) The BitField enumeration (BFType) does not assign contiguous values
        /// 2) Not all bit field enumerations are used
        /// </summary>
        /// <remarks> Will print a debug console message if a bitfield is not defined.</remarks>
        void VerifyBitFields();

        /// <summary>
        /// Fields exposes the internal collection of BitFields ... This is intended ONLY
        /// for use by RegFactory to dynamically create registers.
        /// </summary>
        IBitField[] Fields
        {
            get;
            set;
        }

        #endregion BitField support

        #region performance enhancements

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.Cmd) != 0
        /// </summary>
        bool IsCommand
        {
            get;
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.Event) != 0
        /// </summary>
        bool IsEvent
        {
            get;
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.WO) != 0
        /// </summary>
        bool IsWriteOnly
        {
            get;
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.RO) != 0
        /// </summary>
        bool IsReadOnly
        {
            get;
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.VolatileRw) != 0
        /// </summary>
        bool IsVolatileReadWrite
        {
            get;
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.NoForce) != 0
        /// </summary>
        bool IsNoForce
        {
            get;
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.NoForce) == 0
        /// </summary>
        bool IsForceAllowed
        {
            get;
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.NoValue) != 0
        /// </summary>
        bool IsNoValue
        {
            get;
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.NoValueFilter) != 0
        /// </summary>
        bool IsNoValueFilter
        {
            get;
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.CannotReadDirectly) != 0
        /// </summary>
        bool IsCannotReadDirectly
        {
            get;
        }

        #endregion performance enhancements
    }

    [FlagsAttribute]
    public enum RegType : short
    {
        RW = 0x1,
        RO = 0x2,
        WO = 0x4,

        /// <summary>
        /// CannotReadDirectly registers can not be directly read.
        /// </summary>
        CannotReadDirectly = 0x8,

        /// <summary>
        /// A Cmd reg has special behavior.  If you do a bitfield write,
        /// the register will be written with ONLY that single bitfield as
        /// a 1. If you want to write multiple bitfields at once, you must
        /// combine the bitfields prior to calling Write or Value/Apply
        /// </summary>
        Cmd = 0x10,

        /// <summary>
        /// The significance of this register type is that we DO NOT
        /// want these as part of the group reg apply (especially a force 
        /// apply - since it could erase the flash!).  However, to provide
        /// access to these regs in the GUI, the only other choice was a cmd
        /// register.  But a cmd register has special bit field write behavior
        /// (see above) which would break the flash driver. So we the only 
        /// other choice is for the GUI to recognize these registers as
        /// So this type is used ONLY
        /// by the GUI to know to enable an individual 'write' button for 
        /// this register and disable the (group) Apply button, since the
        /// API does not set this register as part of the register apply. 
        /// </summary>
        Flash = 0x20,

        /// <summary>
        /// These registers are unique in that writing a 1 to any
        /// bit will clear that bit to a zero. If you do a bitfield write,
        /// the register will be written with ONLY that single bitfield as
        /// a 1. These should not be in the
        /// Apply method since the GUI will set these separately.
        /// </summary>
        Event = 0x40,

        /// <summary>
        /// Since these are not part of the apply, the App needs a way to 
        /// identify these registers so that it can provide an individual 
        /// register based Apply() button. 
        /// </summary>
        Int = 0x80,

        /// <summary>
        /// Registers of this type have their value read from HW prior setting a field in the register.
        /// A register that is coupled and subordinate to another register should
        /// be typed as volatile so that changes to the superordinate register are not
        /// lost when writing a field in the subordinate register
        /// </summary>
        VolatileRw = 0x100,

        /// <summary>
        /// This register is a buffer (i.e. an array of bytes or words).  Buffer implementations
        /// typically do not support Apply()
        /// </summary>
        Buffer = 0x200,

        /// <summary>
        /// Any registers with this bit set will be marked "dirty" when any value is written to
        /// the register even if the value did not change.
        /// </summary>
        NoValueFilter = 0x400,

        /// <summary>
        /// Registers with this bit set do not support Apply() -- the register must be written using
        /// Write(x) instead of Value=x;Apply().  One (desired!) implication is these registers
        /// are unaffected by Apply( true ) which is normally used to force a synchronization of
        /// hardware to the register backing store.
        /// </summary>
        NoValue = 0x800,

        /// <summary>
        /// Registers with this bit set do not support Apply() with forceAll true -- the register is
        /// written by Apply only if the value has been explicitly set. Registers with this bit set
        /// should not be marked at construction as needing Apply().
        /// </summary>
        NoForce = 0x1000,

        /// <summary>
        /// Registers with this bit set are initialized when constructed by reading the hardware.
        /// This flag is only checked during construction
        /// </summary>
        InitializeAtCreation = 0x2000
    }
}