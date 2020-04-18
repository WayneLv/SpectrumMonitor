/******************************************************************************
 *                                                                         
 *               
 *              
 *
 *****************************************************************************/
using System;
using System.Runtime.InteropServices;

namespace InstrumentDriver.Core.Interfaces
{
    [ComVisible( true ), System.Reflection.ObfuscationAttribute( Exclude = true )]
    public interface ICoreSettings
    {
        // NOTE: The parameter names in this interface may match the names of methods in other interfaces... Due to
        // NOTE: a bug in the TLH generation, the parameters must have the same casing (i.e. UpperCamel) as the
        // NOTE  methods else the build mistakenly changes the TLH methods (e.g. get_serialNumber instead of
        // NOTE  get_SerialNumber)

        /// <summary>
        /// Sets module properties to their default values.
        /// </summary>
        /// <remarks>
        /// You must call <see cref="Apply()"/>to apply the new property settings 
        /// to hardware.
        /// Reset does not modify properties that control clocks such as 100 MHz Output enables. 
        /// </remarks>
        void Reset();

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
        [ComVisible( false )]
        void Reset( bool SetDefaultRegValues );

        /// <summary>
        /// Reset all the properties in this object and all of the objects aggregated by this
        /// object (e.g. ABus, trigger, etc.) to their default values.  The property values are
        /// NOT written the the corresponding registers (call Apply() to do that)
        /// </summary>
        [ComVisible( false )]
        void ResetProperties();

        /// <summary>
        /// Reset the "set and forget" registers to default values. These are not affected
        /// by properties but normally needed for the module to operate correctly.
        /// 
        /// Default implementation is a NOP
        /// </summary>
        [ComVisible( false )]
        void ResetDefaultRegValues();

        /// <summary>
        /// Module specific validation of the current property state (i.e. the state captured by the backing
        /// variables to properties "managed" by SettingsBase/mPropertyChangePending etc.).  Normally this is
        /// called from SettingsBase.Apply() prior to calling UpdateRegValues.  If the state is invalid and
        /// the settings should not be passed along to the hardware, this method should throw an exception.
        /// </summary>
        /// <exception cref="ValidateFailedException"> Thrown if a modules property combinations are not valid.</exception>
        void Validate();

        /// <summary>
        /// Indicate if any property in this object or any of the objects aggregated by this object (e.g. ABus,
        /// trigger, etc.) have changed as indicated by mPropertyChangePending and need to be written to registers.
        /// </summary>
        [ComVisible( false )]
        bool AnyPropertyChangePending
        {
            get;
        }

        /// <summary>
        /// Returns true if Any Reg Settings in HW do not reflect the most current state of
        /// the Reg Values in SW.
        /// 
        /// The default implementation is just a protected set and public get; 
        /// </summary>
        [ComVisible( false )]
        bool AnyRegSettingsDirty
        {
            get;
        }

        /// <summary>
        /// Indicates that Reg Settings in hardware now match Reg values.  Is intended
        /// to be called after Reg Values are 'Applied' to hardware. 
        ///
        /// The default implementation simply calls AnyRegSettingsDirty = false;
        /// </summary>
        [ComVisible( false )]
        void ClearDirtyRegSettings();

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
        [ComVisible( false )]
        void UpdateRegValues( bool ForceApply );

        /// <summary>
        /// Write all cached register values for this object and all of the objects aggregated by this
        /// object (e.g. ABus, trigger, etc.) to hardware.  This is normally called from SettingsBase.Apply
        /// after calling Validate() and UpdateRegValues()
        /// </summary>
        /// <param name="ForceApply">Can force the apply of register values to hardware even if hardware is not dirty.</param>
        [ComVisible( false )]
        void ApplyRegValuesToHw( bool ForceApply );

        /// <summary>
        /// Provides the same functionality as <see cref="ICoreSettings.Apply()" /> but with the
        /// addition of a "ForceApply" flag.
        /// </summary>
        /// <param name="ForceApply">Forces computation of the hardware settings for
        /// all properties and applies them to hardware regardless of dirty flag settings.</param>
        /// <exception cref="ValidateFailedException"> Thrown if a modules property 
        /// combinations are not valid.</exception>
        void Apply( bool ForceApply );

        /// <summary>
        /// For any property that has changed since the last Apply, compute the 
        /// associated register settings and apply them to hardware. </summary>
        /// <remarks> Property settings are not immediately applied to hardware. They
        /// are only applied to hardware by calling this Apply method.</remarks>
        /// <exception cref="ValidateFailedException"> Thrown if a modules property 
        /// combinations are not valid.</exception>
        void Apply();

        /// <summary>
        /// Add a subscriber to change notification (equivalent to 'event += handler').  It is
        /// up to each module to determine what changes generate notification -- normally only
        /// those changes that may require action from other modules generate notifications.
        /// </summary>
        /// <param name="Subscriber"></param>
        [ComVisible( false )]
        void AddChangeNotifySubscriber( SettingsChangeNotifyDelegate Subscriber );

        /// <summary>
        /// Remove a subscriber from change notification (equivalent to 'event -= handler').
        /// </summary>
        /// <param name="Subscriber"></param>
        [ComVisible( false )]
        void RemoveChangeNotifySubscriber( SettingsChangeNotifyDelegate Subscriber );
    }

    /// <summary>
    /// Delegate for ICoreSettings.AddChangeNotifySubscriber.
    /// </summary>
    /// <param name="Source">The object that generated the event, typically an IInstrument</param>
    /// <param name="Args">Describes the event ... typically the consumer will need to cast the args to a specific class</param>
    [ComVisible( false )]
    public delegate void SettingsChangeNotifyDelegate( object Source, EventArgs Args );
}