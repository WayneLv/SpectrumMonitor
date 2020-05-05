/******************************************************************************
 *                                                                         
 *               
 *               All rights reserved.
 *
 *****************************************************************************/

using System;
using System.Collections;
using System.Diagnostics;
using InstrumentDriver.Core.Utility;
using InstrumentDriver.Core.Utility.Log;

namespace InstrumentDriver.Core.Register
{
    /// <summary>
    /// The base class for All 'Reg' classes. 
    /// </summary>
    /// <remarks>  Contains the parameters needed to uniquely 
    /// identify a register and provides the methods necessary to add BitField instances that
    /// are used to access BitFields within the register.
    /// 
    /// The parameters necessary to uniquely identify a (memory mapped) Register are: 
    /// 1) FPGA/PXIeModule the register is in
    /// 2) offset in BAR space.
    /// 3) BAR (determined by IRegDriver)
    /// 
    /// Because registers are involved in literally millions of operations (register read/write)
    /// it is imperative that the implementation is as efficient as possible -- so we exclude
    /// this class from control flow obfuscation
    /// </remarks>
    [System.Reflection.ObfuscationAttribute( Feature = "controlflow=false" )]
    public abstract class RegBase : IRegister, IEnumerator, IEnumerable

    {
        #region member variables

        protected object mResource = new object();

        protected readonly string mName;
        protected bool mDirty;
        protected IDirtyBit mGroupDirtyBit;
        protected RegType mRegType;
        protected Type mBFType; // the enumerated type for the bitfields this reg contains
        protected RegSet mRegSet; // this is set ONLY if the register is part of a reg set.
        protected IRegDriver mDriver;
        protected IBitField[] mFields;
        // IEnumerable requires these
        private int mIEnumerableIndex;

        // these fields uniquely describe this FPGA register.
        protected Int32 mOffset; // location of the Reg in BAR space.   
        protected int mNumBFs; // number of Bitfields contained in this register
        protected int mFirstBFvalue = -1; // sometimes the enumerated bit field list does not start with 0
        // due to sharing part  of bitfield enumerated lists between
        // registers.

        protected static ILogger mLogger = LogManager.RootLogger;

        // Is this really supposed to be static?
        // protected static LogDelegate_LogItem mLogFunctionLogElement;

        #endregion member variables

        #region constructors

        /// <summary>
        /// Construct a register with RW (read-write) attribute.
        /// </summary>
        /// <param name="name">register name</param>
        /// <param name="offset">offset (BAR offset for memory mapped register)</param>
        /// <param name="bfType">enumerated type of BitFields contained by this register.</param>
        /// <param name="driver">The default driver the register uses to read/write.</param>
        protected RegBase( string name, Int32 offset, Type bfType, IRegDriver driver ) :
            this( name, offset, bfType, RegType.RW, driver )
        {
        }

        /// <summary>
        /// Construct a register with the specified register attributes
        /// </summary>
        /// <param name="name">register name</param>
        /// <param name="offset">offset (BAR offset for memory mapped register)</param>
        /// <param name="bfType">enumerated type of BitFields contained by this register.</param>
        /// <param name="regType">register type/attribute (one or more bits from RegType, e.g. Cmd|RO).</param>
        /// <param name="driver">The default driver the register uses to read/write.</param>
        protected RegBase( string name, Int32 offset, Type bfType, RegType regType, IRegDriver driver )
        {
            mName = name;
            mOffset = offset;
            mBFType = bfType;
            mDriver = driver;
            mRegType = regType;
            // Simplify various checks... (the masking operations are taking way longer than expected)
            IsReadOnly = ( mRegType & RegType.RO ) != 0;
            IsWriteOnly = ( mRegType & RegType.WO ) != 0;
            IsVolatileReadWrite = ( mRegType & RegType.VolatileRw ) != 0;
            IsNoForce = ( mRegType & RegType.NoForce ) != 0;
            IsForceAllowed = ( mRegType & RegType.NoForce ) == 0;
            IsNoValue = ( mRegType & RegType.NoValue ) != 0;
            IsNoValueFilter = ( mRegType & RegType.NoValueFilter ) != 0;
            IsCannotReadDirectly = ( mRegType & RegType.CannotReadDirectly ) != 0;
            IsCommand = ( mRegType & RegType.Cmd ) != 0;
            IsEvent = ( mRegType & RegType.Event ) != 0;

            // Mark as dirty unless type is NoForce
            mDirty = IsForceAllowed;
            IsApplyEnabled = true;

            // Be default, registers are NOT memory mapped (typically the memory
            // mapped implementations are Reg32 and Reg64)
            IsMemoryMapped = false;

            // Optionally initialize the cached value from hardware
            if( ( mRegType & RegType.InitializeAtCreation ) != 0 )
            {
// ReSharper disable DoNotCallOverridableMethodsInConstructor
                // I know this is a virtual method -- must be careful in the implementation to only access RegBase objects
                UpdateRegVal();
// ReSharper restore DoNotCallOverridableMethodsInConstructor
            }
        }

        #endregion constructors

        #region implementation of IEnumerator/IEnumerable

        // IEnumerator requires this
        public IEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            if( mFields == null )
            {
                return false;
            }
            mIEnumerableIndex++;
            return ( mIEnumerableIndex < mFields.Length );
        }

        public void Reset()
        {
            mIEnumerableIndex = mFirstBFvalue - 1;
        }

        public object Current
        {
            get
            {
                return mFields[ mIEnumerableIndex ];
            }
        }

        #endregion implementation of IEnumerator/IEnumerable

        #region implementation of IRegister

        /// <summary>
        /// The IRegDriver this register will use when Apply() is called.  A different IRegDriver may
        /// by used if Apply( IRegDriver, ...) is called.
        /// </summary>
        public IRegDriver Driver
        {
            get
            {
                return mDriver;
            }
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
        public object Resource
        {
            get
            {
                return mResource;
            }
            set
            {
                // Insure exclusive access before swapping...
                object oldResource = mResource;
                lock( oldResource )
                {
                    mResource = value;
                }
            }
        }

        /// <summary>
        /// Complete name of the register.   Some implementations prepend the module name and identifier
        /// so the register can be identified in a multi-module system.
        /// </summary>
        public string Name
        {
            get
            {
                return mName;
            }
        }

        /// <summary>
        /// The base name of the register (i.e. the register name not qualified by the module name and
        /// identifier)
        /// </summary>
        public string NameBase
        {
            get
            {
                int i = mName.IndexOf( '_' );
                return ( i > 0 ) ? mName.Substring( i + 1 ) : mName;
            }
        }

        /// <summary>
        /// The interpretation of Offset depends on the type of register...
        /// 
        /// For memory mapped registers, Offset is the offset from the BAR (Base Address Register) and
        /// is one of the VISA I/O arguments  (e.g.  viOut( BAR, Offset, Data ))
        /// 
        /// For simple device registers, Offset is usually the address of the register within the device.
        /// </summary>
        public Int32 Offset
        {
            get
            {
                return mOffset;
            }
        }

        /// <summary>
        /// RegType indicates various attributes of the register such as read-only or volatile.  RegType
        /// is decorated with [Flags], so multiple attributes can be applied (e.g. read-only and volatile)
        /// </summary>
        public RegType RegType
        {
            get
            {
                return mRegType;
            }
        }

        /// <summary>
        /// If IsMemoryMapped==true, this register reads/writes directly to some memory
        /// location (as opposed to interacting with other registers or serial devices).
        /// The main use for this distinction is for refreshing blocks of registers by
        /// performing a single buffer read (see MemoryMappedControlRegDriver.RegRefresh for
        /// an example coding).
        /// </summary>
        public bool IsMemoryMapped
        {
            get;
            set;
        }

        /// <summary>
        /// The size, in bytes, of the register data.
        /// </summary>
        public abstract int SizeInBytes
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
        public abstract int Value32
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
        public abstract long Value64
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
        public abstract int Read32();

        /// <summary>
        /// Reads a Register value from HW. 
        /// The software copy of the registers value is also updated.
        /// If this is called on a 32 bit register, the implementation may
        /// either throw an exception or zero pad the value to 64 bits.
        /// </summary>
        /// <returns></returns>
        public abstract long Read64();

        /// <summary>
        /// Writes a register value to HW.  The software copy of the registers value is updated.
        /// If this is called on a 64 bit register, the implementation may
        /// either throw an exception or zero pad the value to 64 bits.
        /// </summary>
        /// <param name="registerValue">the value to write to HW></param>
        public abstract void Write32( int registerValue );

        /// <summary>
        /// Writes a register value to HW using the specified IRegDriver.
        /// The software copy of the registers value is also updated.
        /// If this is called on a 64 bit register, the implementation may
        /// either throw an exception or zero pad the value to 64 bits.
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="registerValue">the value to write to HW></param>
        public abstract void Write32( IRegDriver driver, int registerValue );

        /// <summary>
        /// Writes a register value to HW.  The software copy of the registers value is updated.
        /// If this is called on a 32 bit register, the implementation may
        /// either throw an exception or truncate the value to 32 bits.
        /// </summary>
        /// <param name="registerValue">the value to write to HW></param>
        public abstract void Write64( long registerValue );

        /// <summary>
        /// Writes a register value to HW using the specified IRegDriver.
        /// The software copy of the registers value is also updated.
        /// If this is called on a 32 bit register, the implementation may
        /// either throw an exception or truncate the value to 32 bits.
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="registerValue">the value to write to HW></param>
        public abstract void Write64( IRegDriver driver, long registerValue );

        /// <summary>
        /// Write register contents to hardware if dirty (out of sync with Hardware).
        /// </summary>
        /// <param name="forceApply">Write contents even if not dirty.</param>
        public abstract void Apply( bool forceApply );

        /// <summary>
        /// Write register contents to hardware if dirty (out of sync with Hardware)
        /// using the specified IRegDriver.
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="forceApply"></param>
        public abstract void Apply( IRegDriver driver, bool forceApply );

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
        public bool IsApplyEnabled
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
        public virtual bool NeedApply
        {
            get
            {
                return ( mRegSet == null ) ? mDirty : mRegSet.NeedApply;
            }
            set
            {
                // If dirty and have a GroupDirtyBit...
                mDirty = value;
                if( value && ( mGroupDirtyBit != null ) )
                {
                    // set it...
                    mGroupDirtyBit.IsDirty = true;
                }
                if( mRegSet != null )
                {
                    mRegSet.NeedApply = value;
                }
            }
        }

        /// <summary>
        /// Synchronize (i.e. read) the hardware to update the cached register value.
        /// If there is a pending/dirty value (ie. Apply hasn't been called) the
        /// pending value is lost and the dirty flag is cleared.
        /// </summary>
        public abstract void UpdateRegVal();

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
        public abstract void UpdateRegVal32( int value );

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
        public abstract void UpdateRegVal64( long value );

        /// <summary>
        /// GroupDirtyBit is an optional way of tracking if any register in a group of registers
        /// is dirty (i.e. Apply() will write the value).  In the default implementation, RegBase,
        /// if NeedApply is set to true, GroupDirtyBit is set to true.  RegBase will never set
        /// GroupDirtyBit to false (that is intended as a application layer operation).
        /// 
        /// NOTE: a register aggregates a single instance of IDirtyBit so if a register is a member
        ///       of multiple groups it will only set the dirty bit for one of them!
        /// </summary>
        public IDirtyBit GroupDirtyBit
        {
            get
            {
                return mGroupDirtyBit;
            }
            set
            {
                mGroupDirtyBit = value;
                if( NeedApply && mGroupDirtyBit != null )
                {
                    mGroupDirtyBit.IsDirty = true;
                }
            }
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
        public abstract void Push();

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
        public abstract void Pop();

        #region Register bit locking methods

        /// <summary>
        /// Prevent register bits from being modified.
        /// </summary>
        /// <param name="lockMask">The desired mask of locked bits></param>
        public abstract void LockBits32(Int32 lockMask);

        /// <summary>
        /// Prevent register bits from being modified.
        /// </summary>
        /// <param name="lockMask">The desired mask of locked bits></param>
        public abstract void LockBits64(Int64 lockMask);

        /// <summary>
        /// Prevent register bits from being modified.
        /// </summary>
        /// <param name="startBit">Starting bit to lock></param>
        /// <param name="bitCount">Number of bits to lock></param>
        public abstract void LockBits(int startBit, int bitCount);

        /// <summary>
        /// Prevent a register from being modified.
        /// </summary>
        public abstract void LockBits();

        /// <summary>
        /// Allow register bits to be modified.
        /// </summary>
        /// <param name="startBit">Starting bit to unlock></param>
        /// <param name="bitCount">Number of bits to unlock></param>
        public abstract void UnlockBits(int startBit, int bitCount);

        /// <summary>
        /// Allow a register to be modified.
        /// </summary>
        public abstract void UnlockBits();

        /// <summary>
        /// Query a register's lock mask.
        /// </summary>
        /// <returns></returns>
        public abstract Int32 GetLockMask32();

        /// <summary>
        /// Query a register's lock mask.
        /// </summary>
        /// <returns></returns>
        public abstract Int64 GetLockMask64();

        #endregion Register bit locking methods

        #region BitField support

        /// <summary>
        /// Returns the type (normally an Enum) of the bit field identifiers.  If this register
        /// does not support fields, returns null
        /// </summary>
        public Type BFType
        {
            get
            {
                return mBFType;
            }
        }

        /// <summary>
        /// Return the index of the first BitField.
        /// 
        /// BitFields are internally stored in an array.  "empty" slots contain a null and "non-empty" slots
        /// contain a BitField.  FirstBF is the index of the first non-empty slot.
        /// </summary>
        public int FirstBF
        {
            get
            {
                return mFirstBFvalue;
            }
        }

        /// <summary>
        /// Return the index of the last BitField.
        /// 
        /// BitFields are internally stored in an array.  "empty" slots contain a null and "non-empty" slots
        /// contain a BitField.  LastBF is the index of the last non-empty slot.
        /// </summary>
        public int LastBF
        {
            get
            {
                return mFirstBFvalue + mNumBFs - 1;
            }
        }

        /// <summary>
        /// Return the index of the first BitField.
        /// 
        /// BitFields are internally stored in an array.  "empty" slots contain a null and "non-empty" slots
        /// contain a BitField.  NumBitFields return (LastBF-FirstBF-1) but it is possible for some of the
        /// entries between these values to be null.
        /// </summary>
        public int NumBitFields
        {
            get
            {
                return mNumBFs;
            }
        }

        /// <summary>
        /// Replace the BitField at index 'i' of the containing array with 'bf'
        /// </summary>
        /// <param name="i">index into containing array</param>
        /// <param name="bf">BitField being inserted</param>
        protected void SetField( int i, IBitField bf )
        {
            mFields[ i ] = bf;
        }

        /// <summary>
        /// Returns a reference to the BitField specified by index i.
        /// 
        /// Some implementations also provide one of the following methods (not part of this interface)
        ///     RegField32 Field( uint );
        ///     RegField64 Field( uint );
        /// </summary>
        public IBitField GetField( uint i )
        {
            if( i > LastBF )
            {
                throw new ApplicationException( "Field index exceeds numBitFieldsFields" );
            }

            return mFields[ i ];
        }

        /// <summary>
        /// Returns a reference to the BitField specified by index i.
        /// </summary>
        public IBitField Field( uint i )
        {
            if( i > LastBF )
            {
                throw new ApplicationException( "Field index exceeds numBitFieldsFields" );
            }

            return mFields[ i ];
        }

        /// <summary>
        /// Returns a reference to the BitField specified by index i.
        /// </summary>
        public IBitField this[uint i]
        {
            get
            {
                return Field( i ); 
            }
        }

        /// <summary>
        /// Adds a Bit Field to a Register
        /// </summary>
        /// <param name="bf">The BitField being added to the register</param>
        /// <remarks>The BitField passed in must be an implementation of the Type.BFType
        /// passed in on construction of the register.  If not, an exception is thrown.</remarks>
        public void AddField( IBitField bf )
        {
            if( mBFType == null )
            {
                throw new Exception( "Can't add bit fields to a register that does not contain bitfields." );
            }

            // remove all characters up to and including the "_" (the RegName) in order to
            //  get just the BitField portion of the name.
            string bfName = bf.Name;
            //int underScoreLocation = bfName.LastIndexOf("_");
            int underScoreLocation = bfName.LastIndexOf( ":", StringComparison.InvariantCultureIgnoreCase );

            int firstCharOfFieldName = underScoreLocation + 1;

            if( firstCharOfFieldName == 0 )
            {
                // no "_" was found, so we must strip out any " "s instead
                firstCharOfFieldName = bfName.LastIndexOf( " ", StringComparison.InvariantCultureIgnoreCase ) +
                                       " ".Length;
            }

            string bfNameOnly = bfName.Substring( firstCharOfFieldName, bfName.Length - firstCharOfFieldName );
            int index;

            // see if the BFname exists in the enum list or not.
            try
            {
                // If it exists we will have it's index position into the containing array.
                index = (int)Enum.Parse( mBFType, bfNameOnly );
            }
            catch
            {
                // the BitField passed in was not implementing one of the valid BitFields
                // for this register.
                throw new ArgumentException( "No enum match found for BitField name '" +
                                             bfName + "'" );
            }

            SetField( index, bf );
        }

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
        public virtual void VerifyBitFields()
        {
            if( mBFType == null )
            {
                return; // no bitfields to verify.
            }

            int i = 0;
            foreach( IBitField rf in mFields )
            {
                if( i >= mFirstBFvalue )
                {
                    if( rf == null )
                    {
                        Debug.Print( "WARNING: unassigned BitField: '{0}' in Reg: '{1}'",
                                     Enum.GetName( mBFType, i ),
                                     mName );
                    }
                }
                i = i + 1;
            }
        }

        /// <summary>
        /// Fields exposes the internal collection of BitFields ... This is intended ONLY
        /// for use by RegFactory to dynamically create registers.
        /// </summary>
        public virtual IBitField[] Fields
        {
            get
            {
                return mFields;
            }
            set
            {
                mFields = value;
                // Determine the BF characteristics
                mFirstBFvalue = 0;
                for( int j = 0; j < mFields.Length; j++ )
                {
                    if( mFields[ j ] != null )
                    {
                        mFirstBFvalue = j;
                        break;
                    }
                }
                mNumBFs = mFields.Length - mFirstBFvalue;
            }
        }

        #endregion BitField support

        #region performance enhancements

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.Cmd) != 0
        /// </summary>
        public bool IsCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.Event) != 0
        /// </summary>
        public bool IsEvent
        {
            get;
            private set;
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.WO) != 0
        /// </summary>
        public bool IsWriteOnly
        {
            get;
            private set;
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.RO) != 0
        /// </summary>
        public bool IsReadOnly
        {
            get;
            private set;
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.VolatileRw) != 0
        /// </summary>
        public bool IsVolatileReadWrite
        {
            get;
            private set;
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.NoForce) != 0
        /// </summary>
        public bool IsNoForce
        {
            get;
            private set;
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.NoForce) == 0
        /// </summary>
        public bool IsForceAllowed
        {
            get;
            private set;
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.NoValue) != 0
        /// </summary>
        public bool IsNoValue
        {
            get;
            private set;
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.NoValueFilter) != 0
        /// </summary>
        public bool IsNoValueFilter
        {
            get;
            private set;
        }

        /// <summary>
        /// Helper property to increase performance by caching (mRegType & RegType.CannotReadDirectly) != 0
        /// </summary>
        public bool IsCannotReadDirectly
        {
            get;
            private set;
        }

        #endregion performance enhancements

        #endregion Implementation of IRegister

        /// <summary>
        /// Should ONLY be called by the RegSet constructor to turn
        /// a register into part of a RegSet.
        /// </summary>
        /// <param name="rs">Reference to the containing register set for a set of registers.</param>
        internal void PutRegInRegSet( RegSet rs )
        {
            mRegSet = rs;
        }
    }
}