/******************************************************************************
 *                                                                         
 *               Copyright 2012-2013 .
 *               All rights reserved.
 *
 *****************************************************************************/
using System.Runtime.InteropServices;
////using InstrumentDriver.Core.Utility;

namespace InstrumentDriver.Core.Utility
{

    #region Error enumeration (useful for mapping errors into IVI drivers)

    /// <summary>
    /// ModularErrorEnum provides an way of identifying errors to systems that need an
    /// integer identifier (such as an IVI-COM driver).
    /// 
    /// The intent is for multiple projects to use this class as a convenient spot for
    /// defining errors.  To minimize the chance for collisions during merges, each
    /// project should "reserve" a spot in the file.  Note that there is no problem
    /// if different projects use the same error numbers since each project should
    /// only use general errors and its own errors.
    /// </summary>
    [ComVisible( true ), System.Reflection.ObfuscationAttribute( Exclude = true )]
    public enum ModularErrorEnums
    {
        // It is VERY useful to define these enumerations in the "address space"
        // of IVI user defined errors... this allows IVI errors to be defined with
        // identical numeric identifiers as these enumerations.
        //
        // For IVI-COM, if an error is passed "directly" to the user (without being
        // "translated", if the error number matches the IVI-COM error definition
        // then there won't be any confusion if the IVI driver reports an error name
        // based on the error number.

        /// <summary>
        // ModularErrorBase (0x80044000) is the base IVI-COM error number for specific IVI drivers (SPECIFIC_ERROR_BASE)
        /// </summary>
        ModularErrorBase = -2147205120, // 0x80044000

        /// <summary>
        /// ClassErrorBase (0x80024000) is the base IVI-COM error number for class IVI drivers (CLASS_ERROR_BASE)
        /// </summary>
        ClassErrorBase = -2147336192, // 0x80024000;

        [Description( "No Error." )] NoError = 0,

        [Description( "Warning:" )] Warning = 0x44000,
        [Description( "Information:" )] Information = 0x44001,

        // ----------------------------------------------------------------------------------------
        // This block of errors (ModularErrorBase+0 ... ModularErrorBase+499) are intended for
        // general errors that could apply to any module (from multiple projects).
        [Description( "Check error queue for errors." )] CheckErrorQueue = ModularErrorBase + 0,
        [Description( "Unable to initialize hardware." )] HardwareInitializationError = ModularErrorBase + 1,
        [Description( "Parameter value is invalid." )] ParameterValidationError = ModularErrorBase + 2,
        // Do not use ModularErrorBase+3 ... reserved for "Unrecognized Error" (defined in IVI driver)
        [Description( "Module validate failed." )] ModuleValidateFailed = ModularErrorBase + 4,
        [Description( "A wait operation was aborted." )] WaitOperationAborted = ModularErrorBase + 5,
        [Description( "Operation aborted." )] OperationAborted = ModularErrorBase + 6,
        [Description( "Validation error." )] ValidationError = ModularErrorBase + 7,
        [Description( "Internal application error." )] InternalApplicationError = ModularErrorBase + 8,
        [Description( "Unsupported feature." )] UnsupportedFeature = ModularErrorBase + 9,
        [Description( "Incompatible software version." )] IncompatibleSoftwareVersion = ModularErrorBase + 10,
        [Description( "File type not recognized." )] FileTypeNotRecognized = ModularErrorBase + 11,
        [Description( "License validation error." )] LicenseValidationError = ModularErrorBase + 12,
        [Description( "Missing clock error." )] MissingClockError = ModularErrorBase + 13,
        [Description( "Verify operation error." )] VerifyOperationError = ModularErrorBase + 14,
        [Description( "Firmware update error." )] FirmwareUpdateError = ModularErrorBase + 15,
        [Description( "Self test failed" )] SelfTestFailed = ModularErrorBase + 18,
        [Description( "Firmware update required." )] FirmwareUpdateRequired = ModularErrorBase + 20,
        [Description( "License system error." )] LicenseSystemError = ModularErrorBase + 26,
        [Description( "Instrument calibration will be due soon." )] InstrumentCalibrationDue = ModularErrorBase + 32,
        [Description( "Instrument calibration has expired." )] InstrumentCalibrationExpired = ModularErrorBase + 33,
        [Description( "Instrument not calibrated." )] InstrumentNotCalibrated = ModularErrorBase + 34,
        [Description( "Module calibration will be due soon." )] ModuleCalibrationDue = ModularErrorBase + 35,
        [Description( "Module calibration has expired." )] ModuleCalibrationExpired = ModularErrorBase + 36,
        [Description( "Module not calibrated." )] ModuleNotCalibrated = ModularErrorBase + 37,
        [Description( "Timeout (hardware)." )] HardwareTimeout = ModularErrorBase + 38,
        [Description( "Timeout (software)." )] SoftwareTimeout = ModularErrorBase + 39,
        [Description( "Memory allocation failure." )] OutOfMemoryError = ModularErrorBase + 40,
        [Description( "Operation error." )] OperationError = ModularErrorBase + 41,
        [Description( "HW Resource allocation failure.)" )] HwResourceNotAvailable = ModularErrorBase + 42,
        [Description( "Can't update FPGA for shared modules" )] FpgaProgrammingError = ModularErrorBase + 43,
        // Please do not append project specific errors here -- look for the appropriate
        // section of this file below...
		[Description("Unrecognized error")]	UnrecognizedError = -2147205117, // Probably shouldn't use this...
        // ----------------------------------------------------------------------------------------

        [Description( "Acquisition Error." )] AcquisitionError = ModularErrorBase + 2000,
        [Description( "Fixture Loss Table Error." )] FixtureLossTableError = ClassErrorBase + 2001,

        [Description( "Unable to load fixture loss table" )] UnableToLoadFixtureLoss = ClassErrorBase + 4,
        [Description( "Unable to save fixture loss table" )] UnableToSaveFixtureLoss = ClassErrorBase + 5,
        [Description( "Invalid parameter setting fixture loss table" )] BadParameterSettingFixtureLoss = ClassErrorBase + 6,
        [Description( "Trying to get an empty fixture loss table" )] TryingToGetEmptyFixtureLossTable = ClassErrorBase + 7,
        [Description( "Amplitude Alignment error occurred" )] AmplitudeAlignmentFailed = ClassErrorBase + 11,
        [Description( "IF Alignment error occurred" )] IfAlignmentFailed = ClassErrorBase + 12,
        [Description( "LO Alignment error occurred" )] LoAlignmentFailed = ClassErrorBase + 13,
        [Description( "High Power Protection Enabled.")] HighPowerProtectionEnabled = ClassErrorBase + 14,
        [Description( "Changing Acquition mode requires and Apply before Arm" )] ModeChangeRequiresApply = ClassErrorBase + 15,
        [Description( "Capture ID is not valid" )] InvalidCaptureId = ClassErrorBase + 47,
        [Description( "The requested data is unavailable" )] DataUnavailable = ClassErrorBase + 48,
        [Description( "Digitizer does not have enough memory for the requested acquisition" )] OutOfAcquisitionMemory = ClassErrorBase + 49,
        [Description( "Failed to allocate memory for the data buffer" )] DataBufferAllocationFailed = ClassErrorBase + 2002,
        [Description( "Acquisition parameters are not consistent for bandwidth and time and could not be set" )] InconsistentAcquisitionParameters = ClassErrorBase + 2003,
        [Description( "FFT size is not a power of two" )] FftSizeNotPowerOfTwo = ClassErrorBase + 2009,
        [Description( "FFT size is less than the minimum size" )] FftSizeLessThanMinimum = ClassErrorBase + 2010,
        [Description( "FFT size is greater than the maximum size" )] FftSizeGreaterThanMaximum = ClassErrorBase + 2011,
        [Description( "Acquisition is not armed for trigger" )] NotArmedForTrigger = ClassErrorBase + 2012,
        [Description( "Acquisition has timed out" )] AcquisitionTimeout = ClassErrorBase + 2013,

        // ----------------------------------------------------------------------------------------
        // This block of errors is intended for project TBD
    }

    #endregion Error enumeration (useful for mapping errors into IVI drivers)
}
