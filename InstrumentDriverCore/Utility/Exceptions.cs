/******************************************************************************
 *                                                                         
 *                .
 *               All rights reserved.
 *
 *****************************************************************************/
using System;
using System.Runtime.InteropServices;
//using InstrumentDriver.Core.Interfaces;
using InstrumentDriver.Core.Settings;

namespace InstrumentDriver.Core.Utility
{

    #region public exceptions

    /// <summary>
    /// Thrown if the error queue is not empty.  Typically the message and/or
    /// the inner exception is the exception that prompted the check (but there
    /// may be other errors/exceptions in the error queue that happened earlier).
    /// </summary>
    public class CheckErrorQueueException : COMException
    {
        public CheckErrorQueueException()
        {
            HResult = (int)ModularErrorEnums.CheckErrorQueue;
        }

        public CheckErrorQueueException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.CheckErrorQueue;
        }

        public CheckErrorQueueException( string msg, Exception inner ) :
            base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.CheckErrorQueue;
        }
    }

    /// <summary>
    /// Thrown if a failure is detected in the method 
    /// <see cref="IInstrument.ValidateModule(ModuleErrors)"/>.
    /// </summary>
    public class HardwareInitializationException : COMException
    {
        public HardwareInitializationException()
        {
            HResult = (int)ModularErrorEnums.HardwareInitializationError;
        }

        public HardwareInitializationException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.HardwareInitializationError;
        }

        public HardwareInitializationException( string msg, Exception inner ) :
            base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.HardwareInitializationError;
        }
    }

    /// <summary>
    /// HardwareTimeoutException is used when hardware has not responded within
    /// the appropriate amount of time (e.g. interrupt did not occur, I/O timeout,
    /// etc.).   Please use this or SoftwareTimeoutException instead of
    /// System.Timeout (so IVI can recognize the HResult value)
    /// </summary>
    public class HardwareTimeoutException : COMException
    {
        public HardwareTimeoutException()
        {
            HResult = (int)ModularErrorEnums.HardwareTimeout;
        }

        public HardwareTimeoutException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.HardwareTimeout;
        }

        public HardwareTimeoutException( string msg, Exception inner ) :
            base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.HardwareTimeout;
        }
    }

    /// <summary>
    /// SoftwareTimeoutException is used when some non-hardware timeout
    /// has expired (e.g. execution of some method took to long). Please
    /// use this or HardwareTimeoutException instead of
    /// System.Timeout (so IVI can recognize the HResult value)
    /// </summary>
    public class SoftwareTimeoutException : COMException
    {
        public SoftwareTimeoutException()
        {
            HResult = (int)ModularErrorEnums.SoftwareTimeout;
        }

        public SoftwareTimeoutException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.SoftwareTimeout;
        }

        public SoftwareTimeoutException( string msg, Exception inner ) :
            base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.SoftwareTimeout;
        }
    }

    /// <summary>
    /// Thrown if a failure is detected in the method 
    /// <see cref="IInstrument.ValidateModule(ModuleErrors)"/>.
    /// </summary>
    public class ModuleValidateFailedException : COMException
    {
        public ModuleValidateFailedException()
        {
            HResult = (int)ModularErrorEnums.ModuleValidateFailed;
        }

        public ModuleValidateFailedException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.ModuleValidateFailed;
        }

        public ModuleValidateFailedException( string msg, Exception inner ) :
            base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.ModuleValidateFailed;
        }
    }

    /// <summary>
    /// Thrown when user aborts a WaitForEvent call.
    /// </summary>
    public class WaitForEventAbortException : COMException
    {
        public WaitForEventAbortException()
        {
            HResult = (int)ModularErrorEnums.WaitOperationAborted;
        }

        public WaitForEventAbortException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.WaitOperationAborted;
        }

        public WaitForEventAbortException( string msg, Exception inner ) :
            base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.WaitOperationAborted;
        }
    }


    /// <summary>
    /// User aborted operation.
    /// /// </summary>
    public class AbortException : COMException
    {
        public AbortException()
        {
            HResult = (int)ModularErrorEnums.OperationAborted;
        }

        public AbortException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.OperationAborted;
        }

        public AbortException( string msg, Exception inner ) :
            base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.OperationAborted;
        }
    }

    /// <summary>
    /// Thrown if a module <see cref="SettingsBase.Validate()"/>
    /// fails. SettingsBase.Validate() may also be called from
    /// <see cref= "SettingsBase.Apply()"/>.
    /// </summary>
    public class ValidateFailedException : COMException
    {
        public ValidateFailedException()
        {
            HResult = (int)ModularErrorEnums.ValidationError;
        }

        public ValidateFailedException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.ValidationError;
        }

        public ValidateFailedException( string msg, Exception inner ) :
            base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.ValidationError;
        }
    }

    /// <summary>
    /// Thrown if a parameter is invalid.
    /// </summary>
    public class InvalidParameterException : COMException
    {
        public InvalidParameterException() : base( String.Empty, (int)ModularErrorEnums.ParameterValidationError )
        {
            HResult = (int)ModularErrorEnums.ParameterValidationError;
        }

        public InvalidParameterException( string msg ) : base( msg, (int)ModularErrorEnums.ParameterValidationError )
        {
            HResult = (int)ModularErrorEnums.ParameterValidationError;
        }

        public InvalidParameterException( string msg, Exception inner ) :
            base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.ParameterValidationError;
        }
    }

    /// <summary>
    /// Thrown if API software is incompatible with module version.
    /// </summary>
    public class SoftwareVersionIncompatibilityException : COMException
    {
        public SoftwareVersionIncompatibilityException()
        {
            HResult = (int)ModularErrorEnums.IncompatibleSoftwareVersion;
        }

        public SoftwareVersionIncompatibilityException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.IncompatibleSoftwareVersion;
        }

        public SoftwareVersionIncompatibilityException( string msg, Exception inner ) :
            base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.IncompatibleSoftwareVersion;
        }
    }

    /// <summary>
    /// Thrown if file type is not expected type.
    /// </summary>
    public class FileTypeNotRecognizedException : COMException
    {
        public FileTypeNotRecognizedException()
        {
            HResult = (int)ModularErrorEnums.FileTypeNotRecognized;
        }

        public FileTypeNotRecognizedException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.FileTypeNotRecognized;
        }

        public FileTypeNotRecognizedException( string msg, Exception inner ) :
            base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.FileTypeNotRecognized;
        }
    }

    /// <summary>
    /// Thrown when 100 MHz reference clock signal is missing.   Check cables. 
    /// </summary>
    public class MissingClockException : COMException
    {
        public MissingClockException()
        {
            HResult = (int)ModularErrorEnums.MissingClockError;
        }

        public MissingClockException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.MissingClockError;
        }

        public MissingClockException( string msg, Exception inner ) : base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.MissingClockError;
        }
    }

    /// <summary>
    /// Thrown when VerifyOperation method detects a missing LO or IF signal.   Check cables. 
    /// </summary>
    public class VerifyOperationFailedException : COMException
    {
        public VerifyOperationFailedException()
        {
            HResult = (int)ModularErrorEnums.VerifyOperationError;
        }

        public VerifyOperationFailedException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.VerifyOperationError;
        }

        public VerifyOperationFailedException( string msg, Exception inner ) : base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.VerifyOperationError;
        }
    }

    /// <summary>
    /// Special Exception object to hold a low priority warning message, so it can be
    /// handled like other exceptions.  These are usually not thrown. 
    /// </summary>
    public class WarningException : COMException
    {
        public WarningException()
        {
            HResult = (int)ModularErrorEnums.Warning;
        }

        public WarningException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.Warning;
        }

        public WarningException( string msg, Exception inner ) : base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.Warning;
        }
    }

    /// <summary>
    /// Special Exception object to hold a low priority information message, so it can be
    /// handled like other exceptions.  These are usually not thrown. 
    /// </summary>
    public class InformationException : COMException
    {
        public InformationException()
        {
            HResult = (int)ModularErrorEnums.Warning;
        }

        public InformationException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.Warning;
        }

        public InformationException( string msg, Exception inner ) : base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.Warning;
        }
    }

    public class SelfTestFailedException : COMException
    {
        public SelfTestFailedException()
        {
            HResult = (int)ModularErrorEnums.SelfTestFailed;
        }

        public SelfTestFailedException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.SelfTestFailed;
        }

        public SelfTestFailedException( string msg, Exception inner ) : base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.SelfTestFailed;
        }
    }

    ///// <summary>
    ///// An error occurred during writing a .bit file to flash
    ///// </summary>
    public class BitFileProgrammingException : COMException
    {
        public BitFileProgrammingException()
        {
            HResult = (int)ModularErrorEnums.FirmwareUpdateError;
        }

        public BitFileProgrammingException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.FirmwareUpdateError;
        }

        public BitFileProgrammingException( string msg, Exception inner ) : base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.FirmwareUpdateError;
        }
    }

    /// <summary>
    /// An error occurred during writing a .bit file to flash but this error can be overriden
    /// by setting a flag in the flash programming function.
    /// </summary>
    public class OverridableBitFileProgrammingException : COMException
    {
        public OverridableBitFileProgrammingException()
        {
            HResult = (int)ModularErrorEnums.FirmwareUpdateError;
        }

        public OverridableBitFileProgrammingException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.FirmwareUpdateError;
        }

        public OverridableBitFileProgrammingException( string msg, Exception inner ) : base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.FirmwareUpdateError;
        }
    }

    public class HardwareResourceException : COMException
    {
        public HardwareResourceException()
        {
            HResult = (int)ModularErrorEnums.HwResourceNotAvailable;
        }

        public HardwareResourceException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.HwResourceNotAvailable;
        }

        public HardwareResourceException(string msg, Exception inner) : base(msg, inner)
        {
            HResult = (int)ModularErrorEnums.HwResourceNotAvailable;
        }
    }

    #endregion

    #region "internal" exceptions (these are errors we don't expect the user to see!)

    ///// <summary>
    ///// Thrown if a xilinx bit file header section (a,b,c,d,e) is not found, indicating an invalid file.
    ///// </summary>
    public class XilinxHeaderSectionNotFoundException : COMException
    {
        public XilinxHeaderSectionNotFoundException()
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }

        public XilinxHeaderSectionNotFoundException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }

        public XilinxHeaderSectionNotFoundException( string msg, Exception inner ) : base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }
    }


    ///// <summary>
    ///// Xilinx Fpga IDcode is not recognized.
    ///// </summary>
    public class XilinxFpgaIDcodeNotRecognizedException : COMException
    {
        public XilinxFpgaIDcodeNotRecognizedException()
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }

        public XilinxFpgaIDcodeNotRecognizedException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }

        public XilinxFpgaIDcodeNotRecognizedException( string msg, Exception inner ) : base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }
    }

    ///// <summary>
    ///// Board IDcode is not recognized.
    ///// </summary>
    public class BoardIDcodeNotRecognizedException : COMException
    {
        public BoardIDcodeNotRecognizedException()
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }

        public BoardIDcodeNotRecognizedException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }

        public BoardIDcodeNotRecognizedException( string msg, Exception inner ) : base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }
    }

    public class FlashStreamException : COMException
    {
        public FlashStreamException()
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }

        public FlashStreamException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }

        public FlashStreamException( string msg, Exception inner ) : base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }
    }


    public class EventHandlerException : COMException
    {
        public EventHandlerException()
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }

        public EventHandlerException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }

        public EventHandlerException( string msg, Exception inner ) : base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }
    }

    public class ModuleCommunicationException : COMException
    {
        public ModuleCommunicationException()
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }

        public ModuleCommunicationException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }

        public ModuleCommunicationException( string msg, Exception inner ) : base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }
    }

    public class RecordingBufferException : COMException
    {
        public RecordingBufferException()
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }

        public RecordingBufferException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }

        public RecordingBufferException( string msg, Exception inner ) : base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }
    }

    public class ResourceLockingException : COMException
    {
        public ResourceLockingException()
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }

        public ResourceLockingException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }

        public ResourceLockingException( string msg, Exception inner ) : base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }
    }

    /// <summary>
    /// InternalApplicationException is normally used for (hopefully) non-customer visible errors
    /// (i.e. things we should fix before shipping!)
    /// </summary>
    public class InternalApplicationException : COMException
    {
        public InternalApplicationException()
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }

        public InternalApplicationException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }

        public InternalApplicationException( string msg, Exception inner ) : base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.InternalApplicationError;
        }
    }

    public class FixtureLossTableException : COMException
    {
        public FixtureLossTableException()
        {
            HResult = (int)ModularErrorEnums.FixtureLossTableError;
        }
        public FixtureLossTableException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.FixtureLossTableError;
        }
        public FixtureLossTableException( string msg, Exception inner ) : base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.FixtureLossTableError;
        }
    }

    public class AcquisitionException : COMException
    {
        public AcquisitionException()
        {
            HResult = (int)ModularErrorEnums.AcquisitionError;
        }

        public AcquisitionException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.AcquisitionError;
        }

        public AcquisitionException( string msg, Exception inner ) : base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.AcquisitionError;
        }
    }

    public class HighPowerProtectionEnabledException : COMException
    {
        public HighPowerProtectionEnabledException()
        {
            HResult = (int)ModularErrorEnums.HighPowerProtectionEnabled;
        }

        public HighPowerProtectionEnabledException( string msg ) : base( msg )
        {
            HResult = (int)ModularErrorEnums.HighPowerProtectionEnabled;
        }

        public HighPowerProtectionEnabledException(string msg, Exception inner) : base( msg, inner )
        {
            HResult = (int)ModularErrorEnums.HighPowerProtectionEnabled;
        }
    }
    


    #endregion
}