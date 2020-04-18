using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace InstrumentDriver.Core.Utility
{
    /// <summary>
    /// Collection of utility/support methods
    /// </summary>
    public class Support
    {
        #region Keys/Flags to access diagnostic flags

        #region Keys/Flags to access diagnostic flags

        public const string INITIALIZE_KEY = "Initialize";
        public const string DIAGNOSTICS_KEY = "Diagnostics";
        public const string MODULE_DIAGNOSTICS_KEY = "ModuleDiagnostics";
        public const string TIMEOUT_KEY = "Timeout";
        public const string HISTORY_KEY = "History";
        public const string DEFAULT_DIR_KEY = "DefaultDir";
        public const string FLAG_KEY = "Flag";
        public const string FORCE_MODEL_KEY = "ForceModel";
        public const string WRITE_DELAY_KEY = "WriteDelay";
        public const string STACK_TRACE_KEY = "StackTrace";
        public const string DOWNGRADE_OPTIONS_KEY = "DowngradeOptions";
        public const string SIMULATED_SERIAL_NUMBERS_KEY = "SimulatedSerialNumbers";
        public const string BYPASS_ALL_FPGA_PROGRAMMING_CHECKS_KEY = "BypassAllFpgaProgrammingChecks";
        public const string PLUGIN_RAW_FLASH_KEY = "PlugInRawFlash";
        public const string MODEL_NUMBER_BACKUP_KEY = "ModelNumberBackup";
        public const string SHARE_ALL_VISA_SESSIONS_KEY = "ShareAllVisaSessions";
        public const string SHARE_REFERENCE_VISA_SESSION_KEY = "ShareReferenceVisaSession";
        public const string INSTRUMENT_TYPE_KEY = "InstrumentType";
        public const string INSTRUMENT_FILTER_KEY = "InstrumentFilter";
        public const string CONFIGURATION_FILTER_KEY = "ConfigurationFilter";
        public const string USE_DMA_KEY = "UseDMA";
        public const string LOG_ENABLED_KEY = "LogEnabled";
        public const string LOG_LEVEL_KEY = "LogLevel";
        public const string LOG_FLUSH_ON_CLOSE_TO_KEY_KEY = "LogFlushOnCloseTo";
        public const string IGNORE_SHARED_STATE_KEY = "IgnoreSharedState";
        public const string FORCE_INITIALIZE_HARDWARE_KEY = "ForceInitializeHardware";
        public const string USE_LEGACY_V2_RUNTIME_ACTIVATION_POLICY_KEY = "UseLegacyV2RuntimeActivationPolicy";
        public const string MODEL_KEY = "Model";
        public const string IGNORE_MODEL_KEY = "IgnoreModel";
        public const string SKIP_PON_TESTS_KEY = "SkipPonTests";
        public const string SKIP_SLOT_ORDER_TEST_KEY = "SkipSlotOrderTest";
        public const string ERROR_FILTER_LEVEL_KEY = "ErrorFilterLevel";
        public const string UDP_APPENDER_KEY = "UdpAppender";
        public const string UDP_APPENDER_OPTIONS_KEY = "UdpAppenderOptions";
        public const string SKIP_PON_TEST_KEY = "SkipPonTests";
        public const string USE_MEMORY_MAPPED_IO_KEY = "UseMemoryMappedIO";
        public const string FORCE_PON_INITIALIZATION_KEY = "ForcePonInitialization";
        public const string NONVOLATILE_SIMULATION_KEY = "NonvolatileSimulation";
        public const string MOCK_LICENSE_YES_MAN_KEY = "MockLicenseYesMan";
        public const string CVP_REQUIRED_KEY = "CvPRequired";
        public const string ENABLE_CVP_UPDATE_KEY = "EnableCvPUpdate";
        public const string FORCE_CVP_KEY = "ForceCvP";
        public const string MSIX_INITIALIZE_KEY = "MsixInitialize";
        public const string IS_VERSION_MISMATCH_FATAL_KEY = "IsVersionMismatchFatal";
        public const string NO_VERSION_CHECK_KEY = "NoVersionCheck";
        public const string UPDATE_CARRIER_FIRMWARE_KEY = "UpdateCarrierFirmware";
        public const string ALLOW_SFP_ATTACH_KEY = "AllowSfpAttach";
        public const string FPGA_DEVICE_VARIANT = "FpgaDeviceVariant";
        public const string VERIFY_PERIPHERY_PROGRAMMING = "VerifyPeripheryProgramming";
        public const string REQUIRE_FULL_IMAGE_KEY = "RequireFullImage";
        public const string VISA_SERVER_NAME_KEY = "VisaSocketServerName";
        public const string VISA_SERVER_PORT_KEY = "VisaSocketServerPort";
        public const string SIMULATE_THERMAL_SHUTDOWN_KEY = "SimulateThermalShutdown";


        #endregion


        #endregion


        /// <summary>
        /// Compare the two specified versions and return:
        ///   0 if equal
        ///   -1 if one is less than two (or error condition)
        ///   +1 if one is greater than two 
        /// Prior to the comparison, a regular expression "filter" is applied to
        /// both 'one' and 'two' to remove any boilerplate
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <returns></returns>
        public static int CompareVersion( string filter, string one, string two )
        {
            // Remove boilerplate so only a hex version is left
            string filtered1 = Regex.Replace( one, filter, String.Empty );
            string filtered2 = Regex.Replace( two, filter, String.Empty );

            // Parse into individual fields to perform hex comparison
            char[] separator = { '.', ',', '_' };
            string[] fields1 = filtered1.Split( separator, StringSplitOptions.RemoveEmptyEntries );
            string[] fields2 = filtered2.Split( separator, StringSplitOptions.RemoveEmptyEntries );

            // Field by field compare (hex)
            int n = Math.Min( fields1.Length, fields2.Length );
            for( int j = 0; j < n; j++ )
            {
                try
                {
                    int d1 = Int32.Parse( fields1[ j ], NumberStyles.HexNumber );
                    int d2 = Int32.Parse( fields2[ j ], NumberStyles.HexNumber );

                    // Unprogrammed FPGAs will return versions of 0.0.0.0 or FF.FF.FF.FF.
                    // Treat FF values as if they were 0.  Note: This assumes that there
                    // will never be a legitimate version of 255.
                    if (d1 == 255) d1 = 0;
                    if (d2 == 255) d2 = 0;

                    if( d1 < d2 )
                    {
                        return -1;
                    }
                    if( d1 > d2 )
                    {
                        return +1;
                    }
                }
                catch( Exception )
                {
                    // Internal error ... 
                    return -1;
                }
            }

            // Must be equal!
            return 0;
        }

        /// <summary>
        /// Parse value for [true|false|0|1]
        /// 
        /// Note that != 0 is considered true.
        /// 
        /// Unparsable values return false.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool ParseBoolean( string value )
        {
            return ParseBoolean( value, false );
        }

        /// <summary>
        /// Parse value for [true|false|0|1]
        /// 
        /// Note that != 0 is considered true.
        /// 
        /// Unparsable values return defaultValue.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static bool ParseBoolean( string value, Boolean defaultValue )
        {
            bool bResult;
            if( Boolean.TryParse( value, out bResult ) )
            {
                return bResult;
            }
            int iResult;
            if( Int32.TryParse( value, out iResult ) )
            {
                return iResult != 0;
            }
            return defaultValue;
        }
    }
}
