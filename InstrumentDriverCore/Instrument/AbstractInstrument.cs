/******************************************************************************
 *                                                                         
 *                
 *               All rights reserved.
 *
 *****************************************************************************/

using System;
using InstrumentDriver.Core.Utility.Log;
using InstrumentDriver.Core.Interfaces;
using InstrumentDriver.Core.Register;
using InstrumentDriver.Core.Utility;
using System.Reflection;
using InstrumentDriver.Core.Settings;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using InstrumentDriver.Core.Common.IO;
using InstrumentDriver.Core.Mock;

namespace InstrumentDriver.Core
{
    /// <summary>
    /// </summary>
    public abstract class AbstractInstrument : SettingsBase, IInstrument, IInstrumentService, IDisposable
    {
        #region private fields
        private IRegDriver[] mRegDrivers;
        private IRegManager mRegManager;


        #endregion private fields

        #region constructor
        protected AbstractInstrument(
            Dictionary<string, string> options)
        {
            // set all hardware to dirty (unset) when we first instantiate hardware. 
            SetPropertyChangePending(CorePropertyChanged.All);

            ErrorLog = new ErrorLog("Default");

            

        }
        #endregion constructor

        #region constants

        /// <summary>
        /// Success message is specified in JumpStart4 section 3.11.5
        /// </summary>
        public const string NO_ERRORS = "No error.";

        #endregion constants

        #region private methods
        /// <summary>
        /// Alter results of ToString(RegType) to use '|' instead of comma
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static string Format(RegType type)
        {
            return Regex.Replace(string.Format("{0}", type), ", *", "|");
        }
        #endregion private methods

        #region IInstrument Support

        #region Instrument Properties
        /// <summary>
        /// The firmware revision of the Fundamental (not the IVI-driver).  For the version
        /// of code in each module, use IdentificationInformation.
        /// </summary>
        public string FirmwareRevision
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        /// <summary>
        /// Gets a verbose string with the identification information for the instrument.  If this
        /// is a multi-module instrument the IdentificationInformation for each module is included.
        /// 
        /// This is normally a human readable string with no specific format (i.e. it is not meant
        /// to be machine parsed/interpreted).  Typically this information is displayed in the
        /// about dialog of the SFP. For machine readable information <see cref="InstrumentCapability"/>
        /// </summary>
        public virtual string IdentificationInformation
        {
            get
            {
                // TODO: implement default IdentificationInformation
                return "TODO";
            }
        }
        public virtual IInstrumentService Service
        {
            get { return this as IInstrumentService; }
        }
        public virtual bool IsSimulated { get; protected set; }

        public virtual string Options { get; protected set; }

        public abstract string Name { get; }
        public abstract string ModelNumber { get; }
        public abstract string SerialNumber { get; }

        #endregion Instrument Properties

        #region Instrument Methods
        /// <summary>
        /// Query the error queue for any errors that have occurred.  If there are no errors, the code = 0 and message = "No Error"
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        public virtual void ErrorQuery(out Int32 code, out string message)
        {
            // It's conceivable a module doesn't instantiate an error log, so check for that
            if (ErrorLog == null ||
                ErrorLog.IsEmpty)
            {
                code = 0;
                message = NO_ERRORS;
            }
            else
            {
                // Since the log isn't empty, we expect a non-null result, but check anyway
                ErrorLogItem item = ErrorLog.GetFirstItem(ErrorPriorities.Info);
                if (item == null)
                {
                    code = 0;
                    message = NO_ERRORS;
                }
                else
                {
                    code = item.ErrorCode;
                    message = item.Message;

                    // For diagnostic purposes we optionally expose the StackTrace
                    const string STACK_TRACE_KEY = "StackTrace";
                    if (Support.ParseBoolean(GetValue(STACK_TRACE_KEY)) &&
                        item.Exception != null)
                    {
                        // Quite often we wrap exceptions ... the "innermost" InnerException is almost
                        // always the correct one (i.e. the root cause of the exception)
                        Exception ex = item.Exception;
                        while (ex.InnerException != null)
                        {
                            ex = ex.InnerException;
                        }
                        if (ex.StackTrace != null)
                        {
                            message = string.Format("{0}\n{1}", message, ex.StackTrace);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Restores the instrument properties back to their defaults but does not invoke Apply().
        /// This is generally less "expensive" operation than a "Reset".
        /// 
        /// Values are not written to hardware after this call until "Apply" is called.
        /// </summary>
        public virtual void RestoreDefaultProperties()
        {
            // Should be done down in concrete modules.
        }

        public virtual void SelfTest(out int Code, out string Message)
        {
            Code = 0;
            Message = "Selftest passed.";
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
        public virtual void Initialize()
        {
            //Do something after construction end, such as post register creation, board creating etc.
            FinishConstruction();


            //Init hardware by registers
            InitializeHardware();

            //Do power on test if needed
            PowerOnTest();


            //Reset  to default
            Reset();


            //Force apply
            Apply(true);
        }

        public abstract void InitializeHardware();

        /// <summary>
        /// Close all I/O and release all references.
        /// 
        /// Close() may call Dispose() if the implementation uses any unmanaged resources. So
        /// the caller should always discard any references to this object immediately after
        /// calling Close()
        /// </summary>
        public virtual void Close()
        {
            // Close VISA session (currently only closed in Dispose())
            Dispose();
        }

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
        public abstract void FinishConstruction();

        // Helper property to implement FinishConstruction as a singleton method
        // so that it is only called once by Initialize().
        public bool IsFinishedConstruction
        {
            get;
            set;
        } = false;


        /// <summary>
        /// Perform a power on test for the module.
        /// 
        /// The power on test actions are intended to be quick and lightweight.
        /// Note:  Activity in the power on test should NOT throw exceptions.  
        /// Use the error queue instead of throwing exceptions.
        /// The power on test is virtual instead of abstract so implementation is not 
        /// forced.
        /// </summary>
        public virtual void PowerOnTest()
        {
        }

        #endregion Instrument Methods

        #region Register Support

        /// <summary> returns reference to PCIe reg driver for 32 bit registers. </summary>
        public IRegDriver RegDriver
        {
            get;
            protected set;
        }

        /// <summary>
        /// Returns the collection of IRegDriver instances, typically
        /// RegDrivers[n] is for BARn.
        /// </summary>
        /// <remarks>
        /// Some (many? most?) modules have a single IRegDriver instance so the
        /// default implementation is 'return { RegDriver };'
        /// </remarks>
        public IRegDriver[] RegDrivers
        {
            get
            {
                if (mRegDrivers == null)
                {
                    mRegDrivers = new[] { RegDriver };
                }
                return mRegDrivers;
            }
            set
            {
                mRegDrivers = value;
            }
        }

        public IRegManager RegManager
        {
            get;
            protected set;
        }


        /// <summary>
        /// Refresh an array of registers with the current hardware value.
        /// </summary>
        /// <param name="regArray">Array of registers to be refreshed from hardware. </param>
        public void RegRefresh(IRegister[] regArray)
        {
            if (regArray != null && regArray.Length > 0)
            {
                // do it this way for RAM driver.
                regArray[0].Driver.RegRefresh(regArray);
            }
        }


        public void RegRefresh(IRegister[] regArray, int startIndex, int endIndex)
        {
            if (regArray != null && regArray.Length > 0)
            {
                regArray[0].Driver.RegRefresh(regArray, startIndex, endIndex);
            }
        }

        /// <summary>
        /// Goes through an array of registers calling Apply on each register. 
        /// </summary>
        /// <param name="regArray">array of registers.</param>
        /// <param name="ForceApply">If true, writes to hardware even if not dirty.</param>
        public void RegApply(IRegister[] regArray, bool ForceApply)
        {
            if (regArray != null)
            {
                RegApply(regArray, 0, regArray.Length - 1, ForceApply);
            }
        }

        /// <summary>
        /// Goes through an array of registers calling Apply on each register. 
        /// </summary>
        /// <param name="regArray">array of registers.</param>
        /// <param name="ForceApply">If true, writes to hardware even if not dirty.</param>
        /// <param name="driver">current driver being used. Typically PXIe or RecordingControl.</param>
        public void RegApply(IRegDriver driver, IRegister[] regArray, bool ForceApply)
        {
            if (regArray != null)
            {
                RegApply(driver, regArray, 0, regArray.Length - 1, ForceApply);
            }
        }

        /// <summary>
        /// Goes through an array of registers calling Apply on each register in the
        /// index range specified. 
        /// </summary>
        public void RegApply(IRegister[] regArray, int startIndex, int endIndex, bool ForceApply)
        {
            if (regArray != null)
            {
                for (int i = startIndex; i <= endIndex; i++)
                {
                    regArray[i].Apply(RegDriver, ForceApply);
                }
            }
        }

        /// <summary>
        /// Goes through an array of registers calling Apply on each register in the
        /// index range specified. 
        /// </summary>
        public void RegApply(IRegDriver driver,
                              IRegister[] regArray,
                              int startIndex,
                              int endIndex,
                              bool ForceApply)
        {
            if (regArray != null)
            {
                if (driver != null)
                {
                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        regArray[i].Apply(driver, ForceApply);
                    }
                }
                else
                {
                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        regArray[i].Apply(ForceApply);
                    }
                }
            }
        }

        #endregion Register Support

        #endregion IInstrument Support

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                RegDriver.Close();
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~AbstractInstrument() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Implementation of IInstrumentService

        #region Register access (service)

        /// <summary>
        /// Return a comma separated list of register group names.
        /// </summary>
        /// <returns></returns>
        public string GetRegisterGroups()
        {
            StringBuilder buffer = new StringBuilder();
            IEnumerable<string> groups = RegManager.ListGroups();

            foreach (var name in groups)
            {
                buffer.Append(name).Append(',');
            }

            // Trim terminal comma
            if (buffer.Length > 0)
            {
                buffer.Length -= 1;
            }
            return buffer.ToString();
        }

        /// <summary>
        /// Return a comma separated list of register names and descriptions. If
        /// there is not an explicit description, the address of the register
        /// (in hex) is used (e.g.  "Reg1,0x100,Reg2,0x104,Reg3,0x654...").
        /// 
        /// The available register groups are defined by GetRegisterGroups().
        /// </summary>
        /// <param name="groupName">case-insensitive name of the register group to list, use "*" for all groups</param>
        public string GetRegisterNames(string groupName)
        {
            StringBuilder buffer = new StringBuilder();
            IEnumerable<string> groups = (groupName == "*") ? RegManager.ListGroups() : new[] { groupName };

            foreach (var name in groups)
            {
                IRegister[] registers = RegManager.GetGroup(name);
                foreach (var register in registers)
                {
                    if (register != null)
                    {
                        buffer.Append(string.Format("{0},0x{1:x4},", register.NameBase, register.Offset));
                    }
                }
            }

            // Trim terminal comma
            if (buffer.Length > 0)
            {
                buffer.Length -= 1;
            }
            return buffer.ToString();
        }

        /// <summary>
        /// Return a comma separated list of register names and values.
        /// 
        /// The available register groups are defined by GetRegisterGroups().
        /// </summary>
        /// <param name="groupName">case-insensitive name of the register group to list, use "*" for all groups</param>
        public string GetRegisterValues(string groupName)
        {
            StringBuilder buffer = new StringBuilder();
            IEnumerable<string> groups = (groupName == "*") ? RegManager.ListGroups() : new[] { groupName };

            foreach (var name in groups)
            {
                IRegister[] registers = RegManager.GetGroup(name);
                foreach (var register in registers)
                {
                    if ((register != null) && (register.RegType != RegType.Buffer))
                    {
                        buffer.Append(string.Format("{0}.{1},0x{2:x8}({3}),",
                            name, register.NameBase, register.Value32, register.Value32));
                    }
                }
            }

            // Trim terminal comma
            if (buffer.Length > 0)
            {
                buffer.Length -= 1;
            }
            return buffer.ToString();
        }

        /// <summary>
        /// Return a comma separated list of the size, type, and fields in the specified register.
        /// For each field the name, start bit, and size (in bits) are included.  E.g.
        ///      32,RW,One,0,1,Two,1,1,Three,2,4...
        /// </summary>
        /// <param name="groupName">case-insensitive name of group to search for register in, use "*" for all groups</param>
        /// <param name="name">Name (or hex address) of 32 or 64 bit register</param>
        /// <returns></returns>
        public string GetRegisterDefinition(string groupName, string name)
        {
            int address = -1;
            if (name.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
            {
                address = int.Parse(name.Substring(2), NumberStyles.HexNumber);
            }
            IRegister reg = (address == -1)
                                ? RegManager.FindRegister(groupName, name)
                                : RegManager.FindRegister(address);
            if (reg != null)
            {
                StringBuilder buffer = new StringBuilder();
                int bitSize = 8 * reg.SizeInBytes;

                for (int j = reg.FirstBF; j <= reg.LastBF; j++)
                {
                    IBitField field = reg.GetField((uint)j);
                    if (field != null)
                    {
                        buffer.Append(field.ShortName).Append(',');
                        buffer.Append(field.StartBit).Append(',');
                        buffer.Append(field.Size).Append(',');
                    }
                }

                // Special case for no fields defined...
                if (buffer.Length == 0)
                {
                    return string.Format("{0},{1},Value,0,{0}", bitSize, Format(reg.RegType));
                }

                // Insert size & register type...
                buffer.Insert(0, string.Format("{0},{1},", bitSize, Format(reg.RegType)));

                // Trim terminal comma
                if (buffer.Length > 0)
                {
                    buffer.Length -= 1;
                }
                return buffer.ToString();
            }

            // null register ... placeholder
            return "32,RW,Value,0,32";
        }

        /// <summary>
        /// Perform a module-specific refresh operation.  The typical/intended use to to
        /// synchronize registers that may be altered by the FPGA (i.e. shadow copies are
        /// stale).  For TLO common carrier derived modules, this typically executes
        /// CommonReg.Command.Field( CommandBF.CmdAbort ).Write( 1 );
        /// </summary>
        public virtual void RegisterRefresh()
        {
            // NOP
        }

        /// <summary>
        /// Read the register specified by address. If the register is defined as a 64 bit
        /// register, the low 32 bits of the register are returned.
        /// </summary>
        /// <param name="address">address of register</param>
        /// <returns>content of register - size and interpretation depend on register</returns>
        public long ReadRegister(int address)
        {
            // Find the register ...
            IRegister reg = RegManager.FindRegister(address);
            if (reg == null)
            {
                // special case ... register not defined, but since we know the address
                // use the VISA session directly
                return RegDriver.RegRead(address);
            }
            // Special handling...
            if ((reg.RegType & (RegType.WO | RegType.CannotReadDirectly | RegType.VolatileRw)) == 0)
            {
                // Refresh 
                reg.UpdateRegVal();
            }
            if (reg.SizeInBytes == sizeof(int))
            {
                return reg.Value32;
            }
            if (reg.SizeInBytes == sizeof(long))
            {
                return reg.Value64;
            }
            throw new InternalApplicationException(
                string.Format("Cannot read register '{0}', unhandled type/size: {1}, {2}",
                               reg.Name,
                               reg.GetType(),
                               reg.SizeInBytes));
        }

        public string ReadRegisterAsString(int address)
        {
            // Find the register ...
            IRegister reg = RegManager.FindRegister(address);
            if (reg == null)
            {
                // special case ... register not defined, but since we know the address
                // use the VISA session directly
                return string.Format("0x{0:x}", RegDriver.RegRead(address));
            }
            // Special handling...
            if ((reg.RegType & (RegType.WO | RegType.CannotReadDirectly | RegType.VolatileRw)) == 0)
            {
                // Refresh 
                reg.UpdateRegVal();
            }
            return string.Format("0x{0:x}", reg.Value64);
        }

        /// <summary>
        /// Read the register specified by name. If name begins with "0x" it will be parsed
        /// as hexadecimal and the operation delegated to ReadRegister. Otherwise, name is 
        /// "dereferenced" to an address and the operation delegated to ReadRegister. If the
        /// register is defined as a 32 bit register, only the low 32 bits are significant.
        /// </summary>
        /// <param name="groupName">case-insensitive name of group to search for register in, use "*" for all groups</param>
        /// <param name="name">name or hex address of register</param>
        /// <returns>content of register - size and interpretation depend on register</returns>
        public long ReadRegisterByName(string groupName, string name)
        {
            // "Re-direct" hex values (e.g. "0x1234") to ReadRegister( int, ...)
            if (name.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
            {
                return ReadRegister(int.Parse(name.Substring(2), NumberStyles.HexNumber));
            }

            // Find the register ...
            IRegister reg = RegManager.FindRegister(groupName, name);
            if (reg == null)
            {
                // can't find 32 or 64 bit register
                throw new InvalidParameterException(string.Format("Unrecognized register name: {0}", name));
            }
            // Special handling...
            if ((reg.RegType & (RegType.WO | RegType.CannotReadDirectly | RegType.VolatileRw | RegType.Buffer)) == 0)
            {
                // Refresh 
                reg.UpdateRegVal();
            }
            if (reg.SizeInBytes == sizeof(int))
            {
                return reg.Value32;
            }
            if (reg.SizeInBytes == sizeof(long))
            {
                return reg.Value64;
            }
            throw new InternalApplicationException(
                string.Format("Cannot read register '{0}', unhandled type/size: {1}, {2}",
                               reg.Name,
                               reg.GetType(),
                               reg.SizeInBytes));
        }

        public string ReadRegisterByNameAsString(string groupName, string name)
        {
            // "Re-direct" hex values (e.g. "0x1234") to ReadRegister( int, ...)
            if (name.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
            {
                return ReadRegisterAsString(int.Parse(name.Substring(2), NumberStyles.HexNumber));
            }

            // Find the register ...
            IRegister reg = RegManager.FindRegister(groupName, name);
            if (reg == null)
            {
                // can't find 32 or 64 bit register
                throw new InvalidParameterException(string.Format("Unrecognized register name: {0}", name));
            }
            // Special handling...
            if ((reg.RegType & (RegType.WO | RegType.CannotReadDirectly | RegType.VolatileRw | RegType.Buffer)) == 0)
            {
                // Refresh 
                reg.UpdateRegVal();
            }

            return string.Format("0x{0:x}", reg.Value64);
        }

        /// <summary>
        /// Write value to the specified register. If the register is defined as a
        /// 32 bit register, only the low 32 bits of Value are used.
        /// </summary>
        /// <param name="reg">the register to write to</param>
        /// <param name="value">new content of register - size and interpretation depend on register</param>
        /// <param name="force">if true, the register value is written even if it has not changed</param>
        protected void WriteRegister(IRegister reg, long value, bool force)
        {
            if ((reg.RegType & RegType.NoValue) != 0)
            {
                // Special handling...
                // In spite of the desire to have Value=x;Apply() do the same thing as Write(x),
                // here is an exception ... write the value using Write() because Apply() is a NOP
                if (reg.SizeInBytes == sizeof(int))
                {
                    reg.Write32(RegDriver, (int)value);
                }
                else if (reg.SizeInBytes == sizeof(long))
                {
                    reg.Write64(RegDriver, value);
                }
                else
                {
                    throw new InternalApplicationException(
                        string.Format("Cannot write to register '{0}', unhandled type/size: {1}, {2}",
                                       reg.Name,
                                       reg.GetType(),
                                       reg.SizeInBytes));
                }
            }
            else
            {
                // Normal handling...
                // Write value and apply
                if (reg.SizeInBytes == sizeof(int))
                {
                    reg.Value32 = (int)value;
                }
                else if (reg.SizeInBytes == sizeof(long))
                {
                    reg.Value64 = value;
                }
                else
                {
                    throw new InternalApplicationException(
                        string.Format("Cannot write to register '{0}', unhandled type/size: {1}, {2}",
                                       reg.Name,
                                       reg.GetType(),
                                       reg.SizeInBytes));
                }

                // NOTE: ActiveDriver takes care of EnablePeerForwarding/DisablePeerForwarding (if required)
                reg.Apply(RegDriver, force);
            }
        }

        /// <summary>
        /// Write value to the register specified by address. If the register is defined as a
        /// 32 bit register, only the low 32 bits of Value are used.
        /// </summary>
        /// <param name="address">address of register</param>
        /// <param name="value">new content of register - size and interpretation depend on register</param>
        /// <param name="force">if true, the register value is written even if it has not changed</param>
        public void WriteRegister(int address, long value, bool force)
        {
            // Be sure to use 'ActiveDriver' instead of 'RegDriver' so WriteRegister participates in register recording.

            // Find the register ...
            IRegister reg = RegManager.FindRegister(address);
            if (reg == null)
            {
                // special case ... register not defined, but since we know the address
                // use the VISA session directly
                RegDriver.RegWrite(address, (int)value);
            }
            else
            {
                WriteRegister(reg, value, force);
            }
        }

        /// <summary>
        /// Write value to the register specified by name.  If name begins with "0x" it will be parsed
        /// as hexadecimal and the operation delegated to WriteRegister. Otherwise, name is 
        /// "dereferenced" to an address and the operation delegated to WriteRegister.  If the register
        /// is defined as a 32 bit register, only the low 32 bits of Value are used.
        /// </summary>
        /// <param name="groupName">case-insensitive name of group to search for register in, use "*" for all groups</param>
        /// <param name="name">name or hex address of register</param>
        /// <param name="value">new content of register - size and interpretation depend on register</param>
        /// <param name="force">if true, the register value is written even if it has not changed</param>
        public void WriteRegisterByName(string groupName, string name, long value, bool force)
        {
            // Be sure to use 'ActiveDriver' instead of 'RegDriver' so WriteRegisterByName participates in register recording.

            // "Re-direct" hex values (e.g. "0x1234") to WriteRegister( int, ...)
            if (name.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
            {
                WriteRegister(int.Parse(name.Substring(2), NumberStyles.HexNumber), value, force);
                return;
            }

            // Find the register ...
            IRegister reg = RegManager.FindRegister(groupName, name);
            if (reg == null)
            {
                // can't find 32 or 64 bit register
                throw new InvalidParameterException(string.Format("Unrecognized register name: {0}", name));
            }

            // Delegate the rest to WriteRegister(...)
            WriteRegister(reg, value, force);
        }

        /// <summary>
        /// Lock bits in the register specified by name.  If Name begins with "0x" it will be parsed
        /// as a hexadecimal address.  If the register is defined as a 32 bit register, only the
        /// low 32 bits of Value are used.
        /// </summary>
        /// <param name="GroupName">Case-insensitive group name to search for 'Name' in, use "*" for all groups</param>
        /// <param name="Name">name or hex address of register</param>
        /// <param name="Value">new content of register - size and interpretation depend on register</param>
        public void LockRegisterByName(string groupName, string name, long value)
        {
            IRegister reg;

            if (name.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
            {
                int address = int.Parse(name.Substring(2), NumberStyles.HexNumber);
                reg = RegManager.FindRegister(address);
            }
            else
            {
                reg = RegManager.FindRegister(groupName, name);
            }

            if (reg == null)
            {
                // can't find 32 or 64 bit register
                throw new InvalidParameterException(string.Format("Unrecognized register name: {0}", name));
            }

            if (reg.SizeInBytes == sizeof(int))
            {
                reg.LockBits32((int)value);
            }
            else if (reg.SizeInBytes == sizeof(long))
            {
                reg.LockBits64(value);
            }
            else
            {
                throw new InternalApplicationException(
                    string.Format("Cannot write to register '{0}', unhandled type/size: {1}, {2}",
                                   reg.Name,
                                   reg.GetType(),
                                   reg.SizeInBytes));
            }
        }

        /// <summary>
        /// Lock all bits in the register specified by name.  If Name begins with "0x" it will be parsed
        /// as a hexadecimal address.
        /// </summary>
        /// <param name="GroupName">Case-insensitive group name to search for 'Name' in, use "*" for all groups</param>
        /// <param name="Name">name of register</param>
        public void LockRegisterByName(string groupName, string name)
        {
            IRegister reg;

            if (name.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
            {
                int address = int.Parse(name.Substring(2), NumberStyles.HexNumber);
                reg = RegManager.FindRegister(address);
            }
            else
            {
                reg = RegManager.FindRegister(groupName, name);
            }

            if (reg == null)
            {
                // can't find 32 or 64 bit register
                throw new InvalidParameterException(string.Format("Unrecognized register name: {0}", name));
            }

            reg.LockBits();
        }

        /// <summary>
        /// Unlock all bits in the register specified by name.  If Name begins with "0x" it will be parsed
        /// as a hexadecimal address.
        /// </summary>
        /// <param name="GroupName">Case-insensitive group name to search for 'Name' in, use "*" for all groups</param>
        /// <param name="Name">name of register</param>
        public void UnlockRegisterByName(string groupName, string name)
        {
            IRegister reg;

            if (name.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
            {
                int address = int.Parse(name.Substring(2), NumberStyles.HexNumber);
                reg = RegManager.FindRegister(address);
            }
            else
            {
                reg = RegManager.FindRegister(groupName, name);
            }

            if (reg == null)
            {
                // can't find 32 or 64 bit register
                throw new InvalidParameterException(string.Format("Unrecognized register name: {0}", name));
            }

            reg.UnlockBits();
        }

        /// <summary>
        /// Return true if any bits in the specified register are locked.  If Name begins with "0x" it will be parsed
        /// as a hexadecimal address.
        /// </summary>
        /// <param name="GroupName">Case-insensitive group name to search for 'Name' in, use "*" for all groups</param>
        /// <param name="RegisterName"></param>
        /// <param name="FieldName"></param>
        public bool RegisterLockedByName(string groupName, string name)
        {
            return (ReadRegisterLockMaskByName(groupName, name) != 0);
        }

        /// <summary>
        /// Read the lock mask of the specified register.  If Name begins with "0x" it will be parsed
        /// as a hexadecimal address.
        /// </summary>
        /// <param name="GroupName">Case-insensitive group name to search for 'Name' in, use "*" for all groups</param>
        /// <param name="Name">name or hex address of register</param>
        public long ReadRegisterLockMaskByName(string groupName, string name)
        {
            IRegister reg;

            if (name.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
            {
                int address = int.Parse(name.Substring(2), NumberStyles.HexNumber);
                reg = RegManager.FindRegister(address);
            }
            else
            {
                reg = RegManager.FindRegister(groupName, name);
            }

            if (reg == null)
            {
                // can't find 32 or 64 bit register
                throw new InvalidParameterException(string.Format("Unrecognized register name: {0}", name));
            }

            return reg.GetLockMask64();
        }

        /// <summary>
        /// Read the specified field of the specified register.
        /// </summary>
        /// <param name="groupName">case-insensitive name of group to search for register in, use "*" for all groups</param>
        /// <param name="registerName"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public long ReadField(string groupName, string registerName, string fieldName)
        {
            int address = -1;
            if (registerName.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
            {
                address = int.Parse(registerName.Substring(2), NumberStyles.HexNumber);
            }
            IRegister reg = (address == -1)
                                ? RegManager.FindRegister(groupName, registerName)
                                : RegManager.FindRegister(address);
            if (reg != null)
            {
                for (int j = 0; j < reg.NumBitFields; j++)
                {
                    IBitField field = reg.GetField((uint)j);
                    if (field != null &&
                        field.ShortName.Equals(fieldName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        if ((reg.RegType & RegType.VolatileRw) == 0)
                        {
                            reg.UpdateRegVal();
                        }
                        return field.Value;
                    }
                }
            }

            // TODO: Declare error?
            return 0;
        }

        /// <summary>
        /// Write Value to the specified field of the specified register.
        /// </summary>
        /// <param name="groupName">case-insensitive name of group to search for register in, use "*" for all groups</param>
        /// <param name="registerName"></param>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void WriteField(string groupName, string registerName, string fieldName, long value)
        {
            // Be sure to use 'ActiveDriver' instead of 'RegDriver' so WriteField participates in register recording.
            int address = -1;
            if (registerName.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
            {
                address = int.Parse(registerName.Substring(2), NumberStyles.HexNumber);
            }
            IRegister reg = (address == -1)
                                ? RegManager.FindRegister(groupName, registerName)
                                : RegManager.FindRegister(address);
            if (reg != null)
            {
                for (int j = 0; j < reg.NumBitFields; j++)
                {
                    IBitField field = reg.GetField((uint)j);
                    if (field != null &&
                        field.ShortName.Equals(fieldName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        // Special handling
                        if ((reg.RegType & RegType.NoValue) != 0)
                        {
                            // In spite of the desire to have Value=x;Apply() do the same thing as Write(x),
                            // here is an exception ... write the value using Write() because Apply() is a NOP
                            field.Write(RegDriver, (int)value);
                        }
                        else
                        {
                            // Normal handling
                            field.Value = (int)value;

                            // NOTE: ActiveDriver takes care of EnablePeerForwarding/DisablePeerForwarding (if required)
                            field.Apply(RegDriver, false);
                        }
                        return;
                    }
                }
            }

            // TODO: Declare error?
            return;
        }

        /// <summary>
        /// Lock the specified field of the specified register.  If RegisterName begins with "0x" it will be parsed
        /// as a hexadecimal address.
        /// </summary>
        /// <param name="GroupName">Case-insensitive group name to search for 'Name' in, use "*" for all groups</param>
        /// <param name="RegisterName"></param>
        /// <param name="FieldName"></param>
        public void LockField(string groupName, string registerName, string fieldName)
        {
            int address = -1;
            if (registerName.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
            {
                address = int.Parse(registerName.Substring(2), NumberStyles.HexNumber);
            }
            IRegister reg = (address == -1)
                                ? RegManager.FindRegister(groupName, registerName)
                                : RegManager.FindRegister(address);
            if (reg != null)
            {
                for (int j = 0; j < reg.NumBitFields; j++)
                {
                    IBitField field = reg.GetField((uint)j);
                    if (field == null) continue;
                    if (field.ShortName.Equals(fieldName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        field.LockBits();
                    }
                }
            }

            // TODO: Declare error?
        }

        /// <summary>
        /// Unlock the specified field of the specified register.  If RegisterName begins with "0x" it will be parsed
        /// as a hexadecimal address.
        /// </summary>
        /// <param name="GroupName">Case-insensitive group name to search for 'Name' in, use "*" for all groups</param>
        /// <param name="RegisterName"></param>
        /// <param name="FieldName"></param>
        public void UnlockField(string groupName, string registerName, string fieldName)
        {
            int address = -1;
            if (registerName.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
            {
                address = int.Parse(registerName.Substring(2), NumberStyles.HexNumber);
            }
            IRegister reg = (address == -1)
                                ? RegManager.FindRegister(groupName, registerName)
                                : RegManager.FindRegister(address);
            if (reg != null)
            {
                for (int j = 0; j < reg.NumBitFields; j++)
                {
                    IBitField field = reg.GetField((uint)j);
                    if (field == null) continue;
                    if (field.ShortName.Equals(fieldName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        field.UnlockBits();
                    }
                }
            }

            // TODO: Declare error?
        }

        /// <summary>
        /// Return true is the specified field of the specified register is locked.  If RegisterName begins
        /// with "0x" it will be parsed as a hexadecimal address.
        /// </summary>
        /// <param name="GroupName">Case-insensitive group name to search for 'Name' in, use "*" for all groups</param>
        /// <param name="RegisterName"></param>
        /// <param name="FieldName"></param>
        public bool FieldLocked(string groupName, string registerName, string fieldName)
        {
            int address = -1;
            if (registerName.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
            {
                address = int.Parse(registerName.Substring(2), NumberStyles.HexNumber);
            }
            IRegister reg = (address == -1)
                                ? RegManager.FindRegister(groupName, registerName)
                                : RegManager.FindRegister(address);
            if (reg != null)
            {
                long lockMask = reg.GetLockMask64();

                for (int j = 0; j < reg.NumBitFields; j++)
                {
                    IBitField field = reg.GetField((uint)j);
                    if (field == null) continue;
                    if (field.ShortName.Equals(fieldName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return ((field.Mask & lockMask) != 0);
                    }
                }
            }

            // TODO: Declare error?

            return false;
        }

        #endregion Register access (service)

        #region Driver/register logging

        // Default the momento to a level that gets register level logging.
        protected static LogLevel mLogLevelMomento = LogLevel.Fine;

        /// <summary>
        /// Gets/Sets the Logging Level of the logger.  This is compatible
        /// with the new-style logger.  Logging can be disabled through this
        /// by setting the LogLevel.Off.
        /// 
        /// This property is NOT part of IModuleService to keep LogLevel
        /// encapsulate/hidded and preserve the packaging structure.
        /// </summary>
        public virtual LogLevel LoggingLevel
        {
            get
            {
                return mLogger.LoggingLevel;
            }
            set
            {
                mLogger.LoggingLevel = value;
            }
        }

        /// <summary>
        /// Gets/Sets the Logging Level of the logger.  This is compatible
        /// with the new-style logger.  Logging can be disabled through this
        /// by setting the LogLevel.Off (0).  The value maps to the LogLevel
        /// enum defined in 'Core' (use 'int' here to keep LogLevel
        /// encapsulated/hidden as well as preserve the packaging structure)
        /// </summary>
        public virtual int DriverLoggingLevel
        {
            get
            {
                return (int)LoggingLevel;
            }
            set
            {
                LoggingLevel = (LogLevel)value;
            }
        }

        /// <summary>
        /// Enable/disable register driver logging.  When enabled transitions from false
        /// to true, the log is initialized (i.e. all previous log information is lost).
        /// When logging is enabled: register, field, and buffer reads and writes are
        /// captured until the buffer is filled.  The maximum buffer size is set by
        /// DriverLogMaxItems.
        /// </summary>
        public virtual bool DriverLogEnabled
        {
            get
            {
                // Delegate to Logging...
                // return Logging.Enabled;
                return mLogger.LoggingLevel != LogLevel.Off;
            }
            set
            {
                // Delegate to Logging...
                // Logging.Enabled = value;
                if (value)
                {
                    mLogger.LoggingLevel = mLogLevelMomento;
                }
                else
                {
                    mLogLevelMomento = mLogger.LoggingLevel;
                    mLogger.LoggingLevel = LogLevel.Off;
                }
            }
        }

        /// <summary>
        /// The maximum number of items captured in the register driver log.
        /// </summary>
        public virtual int DriverLogMaxItems
        {
            get
            {
                // Delegate to Logging...
                // return Logging.MaxItems;
                return mLogger.Capacity;
            }
            set
            {
                // Delegate to Logging...
                // Logging.MaxItems = value;
                mLogger.Capacity = value;
            }
        }

        /// <summary>
        /// Print the register driver log contents to the specified file.  The date-time and ".txt" will
        /// be appended to the file name.  If the file name does not include directory information, the
        /// file will be created in Personal (My Documents)
        /// </summary>
        /// <param name="baseFileName"></param>
        public virtual void DriverLogPrint(string baseFileName)
        {
            // Delegate to Logging...
            mLogger.Flush(baseFileName);
        }

        /// <summary>
        /// Insert the specified value into the register driver log.
        /// </summary>
        /// <param name="value"></param>
        public virtual void DriverLogInsert(string value)
        {
            // Delegate to Logging...
            if (mLogger.LoggingLevel != LogLevel.Off)
            {
                mLogger.LogAppend(new LoggingEvent(LogLevel.Info, value));
            }
        }

        #endregion Driver/register logging

        #region Generic Properties

        /// <summary>
        /// Set a named value. The implementation is module specific and intended to expose low level properties
        /// (but higher than register access) without having to add an IVI property or a Fundamental interface property.
        /// </summary>
        /// <param name="Key">The key of the value to set (case insensitive).  The value of key is dependent on the module.</param>
        /// <param name="Value">The value to set 'key' to. The value is dependent on the implementing module and 'key' (i.e. it may be the string representation of int, double, complex structure, (or string!), etc.).</param>
        public virtual void SetValue(string Key, string Value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the value of key. The implementation is module specific and intended to expose low level properties
        /// (but higher than register access) without having to add an IVI property or a Fundamental interface property. 
        /// </summary>
        /// <param name="Key">The key of the value to set (case insensitive).  The value of key is dependent on the module.</param>
        /// <returns>The value of 'key'. The value is dependent on the implementing module and 'key' (i.e. it may be the string representation of int, double, complex structure, (or string!), etc.).</returns>
        public virtual string GetValue(string Key)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the value of key. The implementation is module specific and intended to expose low level properties
        /// (but higher than register access) without having to add an IVI property or a Fundamental interface property. 
        /// Typically if the key is not defined, string.Empty is returned.
        /// </summary>
        /// <param name="Key">The key of the value to set (case insensitive).  The value of key is dependent on the module.</param>
        /// <param name="DefaultValue">The value to return if 'Key' is not defined</param>
        /// <returns>The value of 'key'. The value is dependent on the implementing module and 'key' (i.e. it may be the string representation of int, double, complex structure, (or string!), etc.).</returns>
        public virtual string GetValue(string Key, string DefaultValue)
        {
            throw new NotImplementedException();
        }

        #endregion Generic Properties

        #endregion
    }
}
