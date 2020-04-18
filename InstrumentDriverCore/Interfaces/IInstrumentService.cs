/******************************************************************************
 *                                                                         
 *                .
 *               All rights reserved.
 *
 *****************************************************************************/

using System.Runtime.InteropServices;

namespace InstrumentDriver.Core.Interfaces
{
    /// <summary>
    /// Instrument level service methods.
    /// </summary>
    [ComVisible( true ), System.Reflection.ObfuscationAttribute( Exclude = true )]
    public interface IInstrumentService
    {
        #region Generic Properties

        /// <summary>
        /// Set a named value. The implementation is instrument specific and intended to expose low level properties
        /// without having to add an IVI property or a Fundamental interface property.
        /// </summary>
        /// <param name="Key">The key of the value to set (case insensitive).  The value of key is dependent on the module.</param>
        /// <param name="Value">The value to set 'key' to. The value is dependent on the implementing module and 'key' (i.e. it may be the string representation of int, double, complex structure, (or string!), etc.).</param>
        void SetValue( string Key, string Value );

        /// <summary>
        /// Get the value of key. The implementation is instrument specific and intended to expose low level properties
        /// without having to add an IVI property or a Fundamental interface property. Typically if the key is not
        /// defined, string.Empty is returned.
        /// </summary>
        /// <param name="Key">The key of the value to set (case insensitive).  The value of key is dependent on the module.</param>
        /// <returns>The value of 'key'. The value is dependent on the implementing module and 'key' (i.e. it may be the string representation of int, double, complex structure, (or string!), etc.).</returns>
        string GetValue( string Key );

        /// <summary>
        /// Get the value of key. The implementation is instrument specific and intended to expose low level properties
        /// without having to add an IVI property or a Fundamental interface property.
        /// </summary>
        /// <param name="Key">The key of the value to set (case insensitive).  The value of key is dependent on the module.</param>
        /// <param name="DefaultValue">The value to return if 'Key' is not defined</param>
        /// <returns>The value of 'key'. The value is dependent on the implementing module and 'key' (i.e. it may be the string representation of int, double, complex structure, (or string!), etc.).</returns>
        string GetValue( string Key, string DefaultValue );

        #endregion Generic Properties

        #region Register access (service)

        /// <summary>
        /// Return a comma separated list of register group names.
        /// </summary>
        /// <returns></returns>
        string GetRegisterGroups();

        /// <summary>
        /// Return a comma separated list of register names and descriptions. If
        /// there is not an explicit description, the address of the register
        /// (in hex) is used (e.g.  "Reg1,0x100,Reg2,0x104,Reg3,0x654...").
        /// 
        /// The available register groups are defined by GetRegisterGroups().
        /// </summary>
        /// <param name="GroupName">case-insensitive name of the register group to list, use "*" for all groups</param>
        string GetRegisterNames(string GroupName);

        /// <summary>
        /// Return a comma separated list of the size, type, and fields in the specified register.
        /// For each field the name, start bit, and size (in bits) are included.  E.g.
        ///      32,RW,One,0,1,Two,1,1,Three,2,4...
        /// </summary>
        /// <param name="GroupName">Case-insensitive group name to search for 'Name' in, use "*" for all groups</param>
        /// <param name="Name">Name (or hex address) of 32 or 64 bit register</param>
        /// <returns></returns>
        string GetRegisterDefinition(string GroupName, string Name);

        /// <summary>
        /// Perform a module-specific refresh operation.  The typical/intended use to to
        /// synchronize registers that may be altered by the FPGA (i.e. shadow copies are
        /// stale).  For TLO common carrier derived modules, this typically executes
        /// CommonReg.Command.Field( CommandBF.CmdAbort ).Write( 1 );
        /// </summary>
        void RegisterRefresh();

        /// <summary>
        /// Read the register specified by address. If the register is defined as a 64 bit
        /// register, the low 32 bits of the register are returned.
        /// </summary>
        /// <param name="Address">address of register</param>
        /// <returns>content of register - size and interpretation depend on register</returns>
        long ReadRegister(int Address);

        string ReadRegisterAsString(int Address);

        /// <summary>
        /// Read the register specified by name.  If name begins with "0x" it will be parsed
        /// as hexadecimal and the operation delegated to ReadRegister. Otherwise, name is 
        /// "dereferenced" to an address and the operation delegated to ReadRegister. If the
        /// register is defined as a 32 bit register, only the low 32 bits are significant.
        /// </summary>
        /// <param name="GroupName">Case-insensitive group name to search for 'Name' in, use "*" for all groups</param>
        /// <param name="Name">name or hex address of register</param>
        /// <returns>content of register - size and interpretation depend on register</returns>
        long ReadRegisterByName(string GroupName, string Name);

        string ReadRegisterByNameAsString(string GroupName, string Name);

        /// <summary>
        /// Write value to the register specified by address. If the register is defined as a
        /// 32 bit register, only the low 32 bits of Value are used.
        /// </summary>
        /// <param name="Address">address of register</param>
        /// <param name="Value">new content of register - size and interpretation depend on register</param>
        /// <param name="Force">if true, the register value is written even if it has not changed</param>
        void WriteRegister(int Address, long Value, bool Force);

        /// <summary>
        /// Write value to the register specified by name.  If name begins with "0x" it will be parsed
        /// as hexadecimal and the operation delegated to WriteRegister. Otherwise, name is 
        /// "dereferenced" to an address and the operation delegated to WriteRegister.  If the register
        /// is defined as a 32 bit register, only the low 32 bits of Value are used.
        /// </summary>
        /// <param name="GroupName">Case-insensitive group name to search for 'Name' in, use "*" for all groups</param>
        /// <param name="Name">name or hex address of register</param>
        /// <param name="Value">new content of register - size and interpretation depend on register</param>
        /// <param name="Force">if true, the register value is written even if it has not changed</param>
        void WriteRegisterByName(string GroupName, string Name, long Value, bool Force);

        /// <summary>
        /// Lock bits in the register specified by name.  If Name begins with "0x" it will be parsed
        /// as a hexadecimal address.  If the register is defined as a 32 bit register, only the
        /// low 32 bits of Value are used.
        /// </summary>
        /// <param name="GroupName">Case-insensitive group name to search for 'Name' in, use "*" for all groups</param>
        /// <param name="Name">name of register</param>
        /// <param name="Value">new lock mask</param>
        void LockRegisterByName(string GroupName, string Name, long Value);

        /// <summary>
        /// Lock all bits in the register specified by name.  If Name begins with "0x" it will be parsed
        /// as a hexadecimal address.
        /// </summary>
        /// <param name="GroupName">Case-insensitive group name to search for 'Name' in, use "*" for all groups</param>
        /// <param name="Name">name of register</param>
        void LockRegisterByName(string GroupName, string Name);

        /// <summary>
        /// Unlock all bits in the register specified by name.  If Name begins with "0x" it will be parsed
        /// as a hexadecimal address.
        /// </summary>
        /// <param name="GroupName">Case-insensitive group name to search for 'Name' in, use "*" for all groups</param>
        /// <param name="Name">name of register</param>
        void UnlockRegisterByName(string GroupName, string Name);

        /// <summary>
        /// Return true if any bits in the specified register are locked.  If Name begins with "0x" it will be parsed
        /// as a hexadecimal address.
        /// </summary>
        /// <param name="GroupName">Case-insensitive group name to search for 'Name' in, use "*" for all groups</param>
        /// <param name="Name"></param>
        bool RegisterLockedByName(string GroupName, string Name);

        /// <summary>
        /// Read the lock mask of the specified register.  If Name begins with "0x" it will be parsed
        /// as a hexadecimal address.
        /// </summary>
        /// <param name="GroupName">Case-insensitive group name to search for 'Name' in, use "*" for all groups</param>
        /// <param name="Name">name of register</param>
        long ReadRegisterLockMaskByName(string GroupName, string Name);

        /// <summary>
        /// Read the specified field of the specified register.
        /// </summary>
        /// <param name="GroupName">Case-insensitive group name to search for 'Name' in, use "*" for all groups</param>
        /// <param name="RegisterName"></param>
        /// <param name="FieldName"></param>
        /// <returns></returns>
        long ReadField(string GroupName, string RegisterName, string FieldName);

        /// <summary>
        /// Write Value to the specified field of the specified register.
        /// </summary>
        /// <param name="GroupName">Case-insensitive group name to search for 'Name' in, use "*" for all groups</param>
        /// <param name="RegisterName"></param>
        /// <param name="FieldName"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        void WriteField(string GroupName, string RegisterName, string FieldName, long Value);

        /// <summary>
        /// Lock the specified field of the specified register.  If RegisterName begins with "0x" it will be parsed
        /// as a hexadecimal address.
        /// </summary>
        /// <param name="GroupName">Case-insensitive group name to search for 'Name' in, use "*" for all groups</param>
        /// <param name="RegisterName"></param>
        /// <param name="FieldName"></param>
        void LockField(string GroupName, string RegisterName, string FieldName);

        /// <summary>
        /// Unlock the specified field of the specified register.  If RegisterName begins with "0x" it will be parsed
        /// as a hexadecimal address.
        /// </summary>
        /// <param name="GroupName">Case-insensitive group name to search for 'Name' in, use "*" for all groups</param>
        /// <param name="RegisterName"></param>
        /// <param name="FieldName"></param>
        void UnlockField(string GroupName, string RegisterName, string FieldName);

        /// <summary>
        /// Return true is the specified field of the specified register is locked.  If RegisterName begins
        /// with "0x" it will be parsed as a hexadecimal address.
        /// </summary>
        /// <param name="GroupName">Case-insensitive group name to search for 'Name' in, use "*" for all groups</param>
        /// <param name="RegisterName"></param>
        /// <param name="FieldName"></param>
        bool FieldLocked(string GroupName, string RegisterName, string FieldName);

        #endregion Register access (service)

        #region Driver/register logging

        /// <summary>
        /// Enable/disable register driver logging.  When enabled transitions from false
        /// to true, the log is initialized (i.e. all previous log information is lost).
        /// When logging is enabled: register, field, and buffer reads and writes are
        /// captured until the buffer is filled.  The maximum buffer size is set by
        /// DriverLogMaxItems.
        /// </summary>
        bool DriverLogEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/Sets the Logging Level of the logger.  This is compatible
        /// with the new-style logger.  Logging can be disabled through this
        /// by setting the LogLevel.Off (0).  The value maps to the LogLevel
        /// enum defined in 'Core' (use 'int' here to keep LogLevel
        /// encapsulated/hidden as well as preserve the packaging structure)
        /// </summary>
        int DriverLoggingLevel
        {
            get;
            set;
        }

        /// <summary>
        /// The maximum number of items captured in the register driver log. Default is 1e6
        /// </summary>
        int DriverLogMaxItems
        {
            get;
            set;
        }

        /// <summary>
        /// Print the register driver log contents to the specified file.  The date-time and ".txt" will
        /// be appended to the file name.  If the file name does not include directory information, the
        /// file will be created in Personal (My Documents)
        /// </summary>
        /// <param name="BaseFileName"></param>
        void DriverLogPrint(string BaseFileName);

        /// <summary>
        /// Insert the specified value into the register driver log.
        /// </summary>
        /// <param name="Value"></param>
        void DriverLogInsert(string Value);

        #endregion Driver/register logging
    }
}