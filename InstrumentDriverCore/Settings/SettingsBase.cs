/******************************************************************************
 *                                                                         
 *               
 *               .
 *
 *****************************************************************************/
using System;
using System.Collections.Generic;

using InstrumentDriver.Core.Interfaces;
using InstrumentDriver.Core.Utility;
using InstrumentDriver.Core.Utility.Log;

namespace InstrumentDriver.Core.Settings
{
    /// <summary>
    /// An Abstract class that defines methods used in taking Property settings to 
    /// Hardware Registers according to a re-usable pattern.  Any object that has property 
    /// settings that go to hardware must implement the interface methods in this class.
    /// The reason this is not part of CommonModule is that there are some Setting groups
    /// (e.g. ABus) that follow the SettingsBase model, but are in themselves not modules.
    /// </summary>
    public abstract class SettingsBase : ICoreSettings
    {
        #region Generic change bit definitions

        /// <summary>
        /// Dirty/changed bits ... Normally when a property changes it sets a bit in mPropertyChangePending
        /// (using SetIfPropertyChanged) to indicate that the corresponding registers need to be updated
        /// when Apply() is called.  PropertyChanged defines all available bits in generic terms (i.e. Bit0,
        /// Bit1, ... Bit31) and each derived class will normally define its own enum to map these to
        /// meaningful names, e.g.
        /// 
        ///     enum MyPropertyChanged { CenterFrequency = PropertyChanged.Bit0, ... }
        /// 
        /// It is the responsibility of each derived class to not use any of the bits used by its parent
        /// class (sorry about that ... but this is what allows the default property management to be
        /// completely contained in this class).
        /// </summary>
        [Flags]
        protected enum CorePropertyChanged : uint
        {
            None = 0,
            All = 0xffffffff,
            Any = 0xffffffff, // same as 'All', but reads better in an "or" condition
            Bit0 = 0x00000001,
            Bit1 = 0x00000002,
            Bit2 = 0x00000004,
            Bit3 = 0x00000008,
            Bit4 = 0x00000010,
            Bit5 = 0x00000020,
            Bit6 = 0x00000040,
            Bit7 = 0x00000080,
            Bit8 = 0x00000100,
            Bit9 = 0x00000200,
            Bit10 = 0x00000400,
            Bit11 = 0x00000800,
            Bit12 = 0x00001000,
            Bit13 = 0x00002000,
            Bit14 = 0x00004000,
            Bit15 = 0x00008000,
            Bit16 = 0x00010000,
            Bit17 = 0x00020000,
            Bit18 = 0x00040000,
            Bit19 = 0x00080000,
            Bit20 = 0x00100000,
            Bit21 = 0x00200000,
            Bit22 = 0x00400000,
            Bit23 = 0x00800000,
            Bit24 = 0x01000000,
            Bit25 = 0x02000000,
            Bit26 = 0x04000000,
            Bit27 = 0x08000000,
            Bit28 = 0x10000000,
            Bit29 = 0x20000000,
            Bit30 = 0x40000000,
            Bit31 = 0x80000000,
        }

        #endregion Generic change bit definitions

        #region member variables

        protected readonly ILogger mLogger = LogManager.RootLogger;
        protected CorePropertyChanged mCorePropertyChangePending;

        /// <summary>
        /// The public Event handler that clients subscribe to.
        /// </summary>
        protected event SettingsChangeNotifyDelegate SettingsChangeEvent;

        #endregion member variables

        protected SettingsBase()
        {
            NeedValidate = true;
        }

        #region Property change management
        /// <summary>
        /// Use this method to change properties and, if changed, set the PropertyChanged bit corresponding
        /// to that property. 
        /// 
        /// This method does NOT call OnChangeNotify (otherwise the subscriber would be innundated with "trivial"
        /// change notifications).  It is up to each property to decide if it should generate change notification
        /// (by calling OnChangeNotify).
        /// 
        /// The typical implementation looks something like:
        /// <code>
        ///    public T MyProperty {
        ///      get { return mMyProperty; } 
        ///      set { SetIfPropertyChanged( ref mMyProperty, value, (CorePropertyChanged)MyPropertyChanged.MyProperty ); }
        ///    }
        ///    public override Apply() {
        ///      ...
        ///      if( PropertyChangePending( (CorePropertyChanged)MyPropertyChanged.MyProperty ) {
        ///        // ... set register ...
        ///        ClearPropertyChanged( (CorePropertyChanged)MyPropertyChanged.MyProperty );
        ///      }
        /// </code>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="memberVar"></param>
        /// <param name="newValue"></param>
        /// <param name="propertyId"></param>
        protected virtual bool SetIfPropertyChanged < T >( ref T memberVar, T newValue, object propertyId )
        {
            if (EqualityComparer<T>.Default.Equals(memberVar, newValue))
            {
                // no changes
                return false;
            }

            memberVar = newValue;
            SetPropertyChangePending( propertyId );
            // anytime a property changes, we need to re-validate.
            NeedValidate = true;
            return true;
        }

        protected virtual bool SetIfPropertyChanged<T>(ref T memberVar, T newValue, CorePropertyChanged propertyId)
        {
            if (EqualityComparer<T>.Default.Equals(memberVar, newValue))
            {
                // no changes
                return false;
            }

            memberVar = newValue;
            SetPropertyChangePending(propertyId);
            // anytime a property changes, we need to re-validate.
            NeedValidate = true;
            return true;
        }

        /// <summary>
        /// Set a bit indicating the corresponding property is dirty.  This normally indicates
        /// that a corresponding register needs to be updated in Apply().
        /// 
        /// If a derived class needs to keep track of more than 32 properties, override this method and
        /// delegate the operation to a method similar to SetPropertyChangePending( CorePropertyChanged )
        /// that tracks more bits...
        /// </summary>
        /// <param name="bit">the bit (or bits) to set</param>
        protected virtual void SetPropertyChangePending( object bit )
        {
            SetPropertyChangePending( (CorePropertyChanged)bit );
        }

        /// <summary>
        /// Set a bit indicating the corresponding property is dirty.  This normally indicates
        /// that a corresponding register needs to be updated in Apply().
        /// 
        /// If a derived class needs to keep track of more than 32 properties, override
        /// SetPropertyChangePending(object) and delegate the operation to a method similar
        /// to this one that tracks more bits...
        /// </summary>
        /// <param name="bit">the bit (or bits) to set</param>
        protected void SetPropertyChangePending( CorePropertyChanged bit )
        {
            mCorePropertyChangePending |= bit;
        }

        /// <summary>
        /// Clear a bit indicating the corresponding property is clean.  This is normally called
        /// from Apply after a corresponding register has been update.
        /// 
        /// If a derived class needs to keep track of more than 32 properties, override this method and
        /// delegate the operation to a method similar to ClearPropertyChangePending( CorePropertyChanged )
        /// that tracks more bits...
        /// </summary>
        /// <param name="bit">the bit (or bits) to clear</param>
        protected virtual void ClearPropertyChangePending( object bit )
        {
            ClearPropertyChangePending( (CorePropertyChanged)bit );
        }

        /// <summary>
        /// Clear a bit indicating the corresponding property is clean.  This is normally called
        /// from Apply after a corresponding register has been update.
        /// 
        /// If a derived class needs to keep track of more than 32 properties, override
        /// SetPropertyChangePending(object) and delegate the operation to a method similar
        /// to this one that tracks more bits...
        /// </summary>
        /// <param name="bit">the bit (or bits) to set</param>
        protected void ClearPropertyChangePending( CorePropertyChanged bit )
        {
            mCorePropertyChangePending &= ~bit;
        }

        /// <summary>
        /// Determine if the property corresponding to 'bit' is dirty.  If dirty, this normally indicates
        /// that a corresponding register needs to be updated in Apply().
        /// 
        /// If a derived class needs to keep track of more than 32 properties, override this method and
        /// delegate the operation to a method similar to PropertyChangePending( CorePropertyChanged )
        /// that tracks more bits...
        /// </summary>
        /// <param name="bit">the bit (or bits) to clear</param>
        protected virtual bool PropertyChangePending( object bit )
        {
            return PropertyChangePending( (CorePropertyChanged)bit );
        }

        /// <summary>
        /// Determine if the property corresponding to 'bit' is dirty.  If dirty, this normally indicates
        /// that a corresponding register needs to be updated in Apply().
        /// 
        /// If a derived class needs to keep track of more than 32 properties, override
        /// PropertyChangePending(object) and delegate the operation to a method similar
        /// to this one that tracks more bits...
        /// </summary>
        /// <param name="bit">the bit (or bits) to clear</param>
        protected bool PropertyChangePending( CorePropertyChanged bit )
        {
            // One possible way to override this method to support more than 32 properties
            // (448 as shown) is:
            //    int index = (int)(bit & 0x000f);
            //    return ( mBigPropertyChangePending[index] & bit & ~0x000f ) != 0;
            return ( ( mCorePropertyChangePending & bit ) != 0 );
        }

        /// <summary>
        /// Indicate if any property in this object or any of the objects aggregated by this object (e.g. ABus,
        /// trigger, etc.) have changed as indicated by mPropertyChangePending and need to be written to registers.
        /// 
        /// Normally derived objects will need to override this property to handle aggregated objects.  E.g.
        /// <code>
        ///     bool changePending = base.AnyPropertyChangePending ||
        ///                          ABus.AnyPropertyChangePending ||
        ///                          Trigger[0].AnyPropertyChangePending;
        /// </code>
        /// 
        /// If a derived class needs to keep track of more than 32 properties, override property and track
        /// the information tracked by SetPropertyChangePending, ClearPropertyChangePending, and
        /// PropertyChangePending.
        /// </summary>
        public virtual bool AnyPropertyChangePending
        {
            get
            {
                return mCorePropertyChangePending != 0;
            }
        }

        #endregion Property change management

        /// <summary>
        /// Module specific validation of the current property state (i.e. the state captured by the backing
        /// variables to properties "managed" by SettingsBase/mPropertyChangePending etc.).  Normally this is
        /// called from SettingsBase.Apply() prior to calling UpdateRegValues.  If the state is invalid and
        /// the settings should not be passed along to the hardware, this method should throw an exception.
        /// </summary>
        /// <exception cref="ValidateFailedException"> Thrown if a module's property combinations are not valid.</exception>
        public virtual void Validate()
        {
            //This should be overriden if any validation is needed
        }

        /// <summary>
        /// Returns true if Any Reg Settings in HW do not reflect the most current state of
        /// the Reg Values in SW.
        /// 
        /// The default implementation is just a protected set and public get; 
        /// </summary>
        public virtual bool AnyRegSettingsDirty
        {
            get;
            protected set;
        }

        /// <summary>
        /// Indicates that Reg Settings in hardware now match Reg values.  Is intended
        /// to be called after Reg Values are 'Applied' to hardware. 
        ///
        /// The default implementation simply calls AnyRegSettingsDirty = false;
        /// </summary>
        public virtual void ClearDirtyRegSettings()
        {
            AnyRegSettingsDirty = false;
        }

        /// <summary>
        /// Apply all changed properties in this object and all of the objects aggregated by this object (e.g. ABus,
        /// trigger, etc.) to the corresponding registers.  This is normally called by SettingsBase.Apply() after
        /// Validate() has been called.   This method does NOT result in the values being written to hardware (the
        /// values are cached in the registers).  Once all the registers are up to date SettingsBase.Apply() will
        /// call ApplyRegValuesToHw() to copy the registers to hardware (this allows the system to implement order
        /// dependencies for writing registers).
        /// </summary>
        /// <param name="ForceApply">Force updating of all register values regardless
        /// of whether the registers are dirty or not.</param>
        /// <remarks> This method clears register value dirty flags after updating the new
        /// register values and then sets the hardware reg dirty flag to indicate the hardware
        /// registers do not match the latest register values.</remarks>
        public abstract void UpdateRegValues( bool ForceApply );


        /// <summary>
        /// Sets module properties to their default values.
        /// </summary>
        /// <remarks>
        /// You must call <see cref="Apply()"/>to apply the new property settings 
        /// to hardware.
        /// Reset does not modify properties that control clocks such as 100 MHz Output enables. 
        /// </remarks>
        public virtual void Reset()
        {
            ResetProperties();
        }

        /// <summary>
        /// Sets module properties to their default values and will optionally set default values
        /// to "set and forget registers".
        /// </summary>
        /// <param name="SetDefaultRegValues">If true, will also set default values to 
        /// "set and forget registers".</param>
        /// <remarks>
        /// The Apply method must be called to apply the new settings to the hardware.
        /// The term "set and forget registers" refers to registers that are set to a
        /// fixed "default" value on module startup and never changed. 
        /// Reset does not modify properties that control clocks such as 100 MHz Output enables. 
        /// </remarks>
        public virtual void Reset( bool SetDefaultRegValues )
        {
            if( SetDefaultRegValues )
            {
                ResetDefaultRegValues();
            }

            ResetProperties();

            //Apply();  Resets DO NOT call the Apply!!
        }

        /// <summary>
        /// Reset all the properties in this object and all of the objects aggregated by this
        /// object (e.g. ABus, trigger, etc.) to their default values.  The property values are
        /// NOT written the the corresponding registers (call Apply() to do that)
        /// </summary>
        public abstract void ResetProperties();

        /// <summary>
        /// Reset the "set and forget" registers to default values. These are not affected
        /// by properties but normally needed for the module to operate correctly.
        /// 
        /// Default implementation is a NOP
        /// </summary>
        public virtual void ResetDefaultRegValues()
        {
            // default is NOP
        }

        /// <summary>
        /// Write all cached register values for this object and all of the objects aggregated by this
        /// object (e.g. ABus, trigger, etc.) to hardware.  This is normally called from SettingsBase.Apply
        /// after calling Validate() and UpdateRegValues()
        /// </summary>
        /// <param name="ForceApply">Can force the apply of register values to hardware even if hardware is not dirty.</param>
        public abstract void ApplyRegValuesToHw( bool ForceApply );


        /// <summary>
        /// Called before Apply() is called.  This allows each PXI module in a system 
        /// to prepare for changes.  Default implementation is a NOP. 
        /// </summary>
        public virtual void BeforeApply()
        {
        }


        /// <summary>
        /// Will return true if Properties for this module have changed and they have not
        /// been validated yet.
        /// </summary>
        public virtual Boolean NeedValidate
        {
            get;
            protected set;
        }

        /// <summary>
        /// Validate the current property settings then for any property that has changed
        /// since the last Apply or optionally all properties (if forceApply==true), compute
        /// the associated register settings and apply them to hardware.
        /// 
        /// NOTE: normally modules should not override this method (it does quite a bit), the
        ///       preferred overrides are UpdateRegValues(bool) and ApplyRegValuesToHw(bool)
        /// </summary>
        /// <param name="forceApply">Forces computation of the hardware settings for
        /// all properties and applies them to hardware regardless of dirty flag settings.</param>
        /// <exception cref="ValidateFailedException"> Thrown if a module's property 
        /// combinations are not valid.</exception>
        public virtual void Apply( bool forceApply )
        {
            if( NeedValidate )
            {
                // Validate has not been called, so we must call it.
                Validate();
                NeedValidate = false;
            }

            // If properties have changed, the reg values that represent them are
            // dirty and need to be updated.
            if( AnyPropertyChangePending || forceApply )
            {
                // this method is module specific, and is overridden in each top level 
                // module class
                UpdateRegValues( forceApply );

                // At this point, all settings should be handled ... since 'All' may
                // be defined as 0xffffffff (for convenience) even when there aren't
                // that many properties, force a clear of all change bits so
                // AnyChangesPending will return false.
                ClearPropertyChangePending( CorePropertyChanged.All );
            }


            // This checks to see if the register values have changed. If they have,
            // the HW reg settings for the reg values are dirty and we need to write 
            // the reg values to HW.
            if( AnyRegSettingsDirty || forceApply )
            {
                // this method is module specific, and is overridden in each top level 
                // module class
                ApplyRegValuesToHw( forceApply );
                // ClearDirtyRegSettings();  // this should be handled for us in ApplyRegValuesToHw()
            }
        }

        /// <summary>
        /// Validate the current property settings then for any property that has changed
        /// since the last Apply, compute the associated register settings and apply them
        /// to hardware.
        /// 
        /// NOTE: normally modules should not override this method (it does quite a bit), the
        ///       preferred overrides are UpdateRegValues(bool) and ApplyRegValuesToHw(bool)
        /// </summary>
        /// <remarks> Property settings are not immediately applied to hardware. They
        /// are only applied to hardware by calling this Apply method.</remarks>
        /// <exception cref="ValidateFailedException"> Thrown if a module's property 
        /// combinations are not valid.</exception>
        public virtual void Apply()
        {
            Apply( false );
        }

        /// <summary>
        /// Add a subscriber to change notification (equivalent to 'event += handler').  It is
        /// up to each module to determine what changes generate notification -- normally only
        /// those changes that may require action from other modules generate notifications.
        /// </summary>
        /// <param name="subscriber"></param>
        public void AddChangeNotifySubscriber( SettingsChangeNotifyDelegate subscriber )
        {
            SettingsChangeEvent += subscriber;
        }

        /// <summary>
        /// Remove a subscriber from change notification (equivalent to 'event -= handler').
        /// </summary>
        /// <param name="subscriber"></param>
        public void RemoveChangeNotifySubscriber( SettingsChangeNotifyDelegate subscriber )
        {
            SettingsChangeEvent -= subscriber;
        }

        /// <summary>
        /// Notify any subscribers of a settings change.  It is up to each module to determine which
        /// changes are broadcast -- typically just those changes that may affect other modules are
        /// sent.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        protected void OnChangeNotify( object source, EventArgs args )
        {
            if( SettingsChangeEvent != null )
            {
                // Some methods/properties called by handling errors don't know they are in an event
                // thread (which can't throw errors back to the user) ... Instead, errors need to be
                // put in the error queue...
                try
                {
                    SettingsChangeEvent( source, args );
                }
                catch( Exception ex )
                {
                    mLogger.LogAppend( new LoggingEvent( LogLevel.Error, ex.Message, ex ) );
                }
            }
        }

        #region ErrorLog support

        /// <summary>
        /// Log of errors and warnings.
        /// </summary>
        /// <remarks>
        /// Most run-time errors are reported by immediately throwing an Exception. 
        /// ErrorLog collects other warnings that do not warrant exceptions, 
        /// and errors encountered during object construction. 
        /// </remarks>
        public IErrorLog ErrorLog
        {
            get;
            set;
        }

        #endregion ErrorLog support
    }
}