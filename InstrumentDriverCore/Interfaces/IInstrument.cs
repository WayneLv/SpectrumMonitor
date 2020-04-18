/******************************************************************************
 *                                                                         
 *                .
 *               All rights reserved.
 *
 *****************************************************************************/

using InstrumentDriver.Core.Register;
using InstrumentDriver.Core.Utility;
using System;
using System.Runtime.InteropServices;

namespace InstrumentDriver.Core.Interfaces
{
    /// <summary>

    /// </summary>
    [ComVisible(true), System.Reflection.ObfuscationAttribute(Exclude = true)]
    public interface IInstrument
    {
        // NOTE: The parameter names in this interface match the names of methods in other interfaces... Due to
        // NOTE: a bug in the TLH generation, the parameters must have the same casing (i.e. UpperCamel) as the
        // NOTE  methods else the build mistakenly changes the TLH methods (e.g. get_serialNumber instead of
        // NOTE  get_SerialNumber)
        #region Configuration

        /// <summary> name. </summary>
        string Name
        {
            get;
        }


        /// <summary>
        /// Indicates if the instrument is simulated. It is possible for a multi-module instrument
        /// to have some modules simulated and others use real hardware (e.g. it may be worthwhile
        /// to iterate over the module collections and check IsSimulated for each module).
        /// 
        /// Note that IVI restricts some transitions (e.g. initialized with simulated=true does
        /// not allow simulated=false).
        /// </summary>
        bool IsSimulated
        {
            get;
        }

        /// <summary>
        /// Get the serial number of the instrument.  
        /// 
        /// If this is a multi-module instrument this is generally a comma delimited list of the
        /// serial numbers of the modules composing the instrument.  The order of the modules is
        /// documented/visible to the end-user as:
        ///    Sorted by model then IModule.UniqueId
        /// This must match the order returned by GetModules().
        /// </summary>
        string SerialNumber
        {
            get;
        }

        /// <summary>
        /// Get the options of the instrument.
        /// 
        /// This is generally a comma delimited list of instrument specific option identifier,
        /// e.g. F06,M05
        /// </summary>
        string Options
        {
            get;
        }

        /// <summary>
        /// Get the model number of the instrument.  
        /// 
        /// </summary>
        string ModelNumber
        {
            get;
        }

        /// <summary>
        /// The firmware revision of the Fundamental (not the IVI-driver).  For the version
        /// of code in each module (e.g. FPGA version), use IdentificationInformation.
        /// </summary>
        string FirmwareRevision
        {
            get;
        }

        
        

        /// <summary>
        /// Log of errors and warnings.
        /// </summary>
        /// <remarks>
        /// Most run-time errors are reported by immediately throwing an Exception. 
        /// ErrorLog collects other warnings that do not warrant exceptions, 
        /// and errors encountered during object construction. 
        /// </remarks>
        IErrorLog ErrorLog
        {
            get;
        }


        /// <summary>
        /// Gets a verbose string with the identification information for the instrument.  If this
        /// is a multi-module instrument the IdentificationInformation for each module is included.
        /// 
        /// This is normally a human readable string with no specific format (i.e. it is not meant
        /// to be machine parsed/interpreted).  Typically this information is displayed in the
        /// about dialog of the SFP. For machine readable information <see cref="InstrumentCapability"/>
        /// </summary>
        string IdentificationInformation
        {
            get;
        }

        /// <summary>
        /// Close all I/O and release all references.
        /// 
        /// Close() may call Dispose() if the implementation uses any unmanaged resources. So
        /// the caller should always discard any references to this object immediately after
        /// calling Close()
        /// </summary>
        void Close();

        #endregion Configuration

        #region Generic instrument control

        /// <summary>
        /// Instrument level service/internal methods.
        /// </summary>
        IInstrumentService Service
        {
            get;
        }

        /// <summary>
        /// This method initializes an instrument back to its power up state. This includes
        /// any necessary hardware initialization as well as resetting instrument properties 
        /// to their Reset state.
        /// 
        /// In general, Initialize() is more "aggressive" than Reset() -- this may perform
        /// operations that are normally intended only for power up sequencing (so it may
        /// result in destroying inter-instrument/module synchronization)
        /// </summary>
        void Initialize();

        /// <summary>
        /// Reset the instrument.  This Restores the instrument properties back to their defaults
        /// and performs an implied Apply().
        /// </summary>
        void Reset();

        /// <summary>
        /// Restores the instrument properties back to their defaults but does not invoke Apply().
        /// This is generally less "expensive" operation than a "Reset".
        /// 
        /// Values are not written to hardware after this call until "Apply" is called.
        /// </summary>
        void RestoreDefaultProperties();

        /// <summary>
        /// Apply pending changes to the instrument configuration.
        /// </summary>
        void Apply();

        /// <summary>
        /// Query the error queue for any errors that have occurred.  
        /// If there are no errors, the code = 0 and message = "No error." (specified in JumpStart4 section 3.11.5)
        /// </summary>
        /// <param name="Code"></param>
        /// <param name="Message"></param>
        void ErrorQuery( out Int32 Code, out string Message );

        /// <summary>
        /// Perform a self test. This is normally invoked by the IVI Utility.SelfTest method.  If there are
        /// no errors the return values must be 0 and "Selftest passed" (sic. this message is dictated by 
        /// Jumpstart 4 and should not be changed to "Self test passed" until JS4 is updated).
        /// </summary>
        /// <param name="Code"></param>
        /// <param name="Message"></param>
        void SelfTest( out Int32 Code, out string Message );

        
        /// <summary>
        /// Module specific validation of the current property state (i.e. the state captured by the backing
        /// variables to properties "managed" by SettingsBase/mPropertyChangePending etc.).  Normally this is
        /// called from SettingsBase.Apply() prior to calling UpdateRegValues.  If the state is invalid and
        /// the settings should not be passed along to the hardware, this method should throw an exception.
        /// </summary>
        /// <exception cref="ValidateFailedException"> Thrown if a modules property combinations are not valid.</exception>
        void Validate();

        /// <summary>
        /// Will return true if Properties for this module have changed and they have not
        /// been validated yet.
        /// </summary>
        Boolean NeedValidate
        {
            get;
        }


        /// <summary>
        /// Initialize the board -- intended to be executed once after power up.
        /// What this actually means/does is board dependent.
        /// </summary>
        void InitializeHardware();

        /// <summary>
        /// FinishConstruction is the part of the constructor code that could fail.  Therefore, it
        /// is refactored out of the constructor into a new method.  This method should only be called 
        /// once after the constructor call.  
        /// Note: Set the IsFinishedConstruction property to true even if an exception is thrown.  
        /// The reason is that when an exception is thrown, we want to be in diagnostic mode, instead of 
        /// attempting to recreate the object.
        /// 
        /// Post-register-creation construction:  this creates the flash driver, flash file system(s),
        /// instances of ICommonBoard (to represent the carrier and optionally plugin), etc.  Every
        /// step is virtualized and hence may be customized as much or as little as needed by this module.
        /// The default implementation creates a set of objects that are compatible with the APeX modules.
        /// It is almost certain that all other modules will need to at least customize the flash file
        /// system(s).
        ///
        /// </summary>
        void FinishConstruction();

        // Helper property to implement FinishConstruction as a singleton method
        // so that it is only called once by Initialize().
        bool IsFinishedConstruction
        {
            get;
            set;
        }

        /// <summary>
        /// Perform a power on test for the module.
        /// 
        /// The power on test actions are intended to be quick and lightweight.
        /// Note:  Activity in the power on test should NOT throw exceptions.  
        /// Use the error queue instead of throwing exceptions.
        /// The power on test is virtual instead of abstract so implementation is not 
        /// forced.
        /// </summary>
        void PowerOnTest();

        #endregion Generic instrument control

        #region Register related
        /// <summary>
        /// returns reference to PCIe reg driver.
        /// </summary>
        IRegDriver RegDriver
        {
            get;
        }

        /// <summary>
        /// Returns the collection of IRegDriver instances, typically
        /// RegDrivers[n] is for BARn.
        /// </summary>
        /// <remarks>
        /// Some (many? most?) modules have a single IRegDriver instance so the
        /// default implementation is 'return { RegDriver };'
        /// </remarks>
        IRegDriver[] RegDrivers
        {
            get;
        }

        /// <summary>
        /// Reference to the container for all module registers.
        /// </summary>
        IRegManager RegManager
        {
            get;
        }

        /// <summary>
        /// Refresh an array of registers with the current hardware value.
        /// </summary>
        /// <param name="regArray">Array of registers to be refreshed from hardware. </param>
        void RegRefresh(IRegister[] regArray);


        void RegRefresh(IRegister[] regArray, int startIndex, int endIndex);

        /// <summary>
        /// Goes through an array of registers calling Apply on each register. 
        /// </summary>
        /// <param name="regArray">array of registers.</param>
        /// <param name="ForceApply">If true, writes to hardware even if not dirty.</param>
        void RegApply(IRegister[] regArray, bool ForceApply);

        /// <summary>
        /// Goes through an array of registers calling Apply on each register. 
        /// </summary>
        /// <param name="driver">current driver being used. Typically PXIe or RecordingControl.</param>
        /// <param name="regArray">array of registers.</param>
        /// <param name="ForceApply">If true, writes to hardware even if not dirty.</param>
        void RegApply(IRegDriver driver, IRegister[] regArray, bool ForceApply);

        /// <summary>
        /// Goes through an array of registers calling Apply on each register in the
        /// index range specified. 
        /// </summary>
        void RegApply(IRegister[] regArray, int startIndex, int endIndex, bool ForceApply);

        /// <summary>
        /// Goes through an array of registers calling Apply on each register in the
        /// index range specified. 
        /// </summary>
        void RegApply(IRegDriver driver,
                       IRegister[] regArray,
                       int startIndex,
                       int endIndex,
                       bool ForceApply);


        #endregion Register related

    }

}
