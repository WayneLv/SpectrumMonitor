using InstrumentDriver.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using InstrumentDriver.Core.Common.IO;
using InstrumentDriver.Core.Utility;
using InstrumentDriver.SpectrumMonitor;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace SpectrumMonitor.ViewModel
{
    public class RegisterControlViewModel : AbstractModel
    {

        private IInstrument mInstr = null;
        private int mSelectedGroupIndex = 0;
        private int mSelectedRegisterIndex = 0;
        private List<string> mRegisterGroupItems = new List<string>();
        private ObservableCollection<Register> mCurrentGroupRegsiters = new ObservableCollection<Register>();
        private ObservableCollection<BitField> mCurrentBitFields = new ObservableCollection<BitField>();


        private const string ADDRESSTYPE_MAPPEDCONTROLREG = "Mapped Control Reg";
        private const string ADDRESSTYPE_CARRIERREG = "Carrier Reg";
        private const string ADDRESSTYPE_SPECTRUMDATAMEMORY = "Spectrum Data Memory";


        private SpctrumMonitorViewModel mMainViewModel;

        private MyAddressSpace mMyAddressSpace = MyAddressSpace.MappedControlReg;

        public RegisterControlViewModel(SpctrumMonitorViewModel mainViewModel)
        {
            mMainViewModel = mainViewModel;
            mInstr = mainViewModel.Instrument;

            var groupstr = mInstr.Service.GetRegisterGroups();

            string[] groups = groupstr.Split(',');
            mRegisterGroupItems.Clear();
            mRegisterGroupItems.AddRange(groups);

            AddressType = new List<string>();
            AddressType.AddRange(new List<string>(){ ADDRESSTYPE_MAPPEDCONTROLREG, ADDRESSTYPE_CARRIERREG, ADDRESSTYPE_SPECTRUMDATAMEMORY });

            mSelectedAddressType = AddressType[0];
            WriteAddress = "0x00";
            ReadAddress = "0x00";
            LengthToRead = "1";

            WriteData = "11,3,44,67,0x78,0x15";
        }

        #region Register access
        public List<string> RegisterGroupItems
        {
            get
            {
                return mRegisterGroupItems;
            }
        }
        public int RegisterGroupIndex
        {
            get
            {
                return mSelectedGroupIndex;
            }
            set
            {

                mSelectedGroupIndex = value;

                mCurrentGroupRegsiters.Clear();
                var namestr = mInstr.Service.GetRegisterNames(CurrentGroup);
                var names = namestr.Split(',');
                for (int i = 0; i < names.Length / 2; i++)
                {
                    Register newreg = new Register();
                    newreg.Name = names[2 * i];
                    newreg.Address = Convert.ToInt64(names[2 * i + 1],16);
                    newreg.IsDisplayValueHex = IsDisplayHex;
                    mCurrentGroupRegsiters.Add(newreg);
                }

                RegisterIndex = 0;

                NotifyPropertyChanged(() => RegisterGroupIndex);
                NotifyPropertyChanged(() => CurrentGroupRegsiters);
            }
        }

        public ObservableCollection<Register> CurrentGroupRegsiters
        {
            get
            {
                return mCurrentGroupRegsiters;
            }
            set
            {
                mCurrentGroupRegsiters = value;
                NotifyPropertyChanged(() => CurrentGroupRegsiters);
            }
        }

        public int RegisterIndex
        {
            get
            {
                return mSelectedRegisterIndex;
            }
            set
            {
                if (value < 0)
                    return;

                mSelectedRegisterIndex = value;

                // Changing the register changes the bit fields! (be sure to clear event handlers!)
                foreach (var item in mCurrentBitFields)
                {
                    item.PropertyChanged -= OnBitFieldChanged;
                }
                mCurrentBitFields.Clear();


                var namestr = mInstr.Service.GetRegisterDefinition(CurrentGroup, CurrentRegister.Name);
                var names = namestr.Split(',');

                CurrentRegister.Size = Convert.ToInt32(names[0]);
                CurrentRegister.Type = names[1];
                CurrentRegType = names[1];
                CurrentRegAddress = String.Format("0x{0:x}", CurrentRegister.Address);
                CurrentRegister.IsDisplayValueHex = IsDisplayHex;


                var valueString = mInstr.Service.ReadRegisterByNameAsString(CurrentGroup, CurrentRegister.Name);
                CurrentRegisterValue = ParseRegValue(valueString, CurrentRegister.Size);

                for (int i = 0; i < (names.Length - 2) / 3; i++)
                {
                    BitField newbf = new BitField();
                    newbf.Name = names[2 + 3 * i];
                    newbf.StartBit = Convert.ToInt32(names[2 + 3 * i + 1]);
                    newbf.Size = Convert.ToInt32(names[2 + 3 * i + 2]);
                    newbf.EndBit = newbf.StartBit - 1 + newbf.Size;
                    newbf.IsDisplayValueHex = IsDisplayHex;
                    newbf.PropertyChanged += OnBitFieldChanged;
                    mCurrentBitFields.Add(newbf);
                }

                //CurrentRegisterValue = CurrentRegister.Value;
                //CurrentRegType = CurrentRegister.Type;

                NotifyPropertyChanged(() => RegisterIndex);
                NotifyPropertyChanged(() => CurrentRegsiterBitFields);

                RefreshBFValue();
            }
        }

        public Register CurrentRegister
        {
            get { return CurrentGroupRegsiters[mSelectedRegisterIndex]; }
        }

        public string CurrentGroup
        {
            get { return RegisterGroupItems[mSelectedGroupIndex]; }
        }

        private void OnBitFieldChanged(object sender, EventArgs args)
        {
            BitField bitField = sender as BitField;
            if (bitField != null)
            {
                if (bitField.Value > Convert.ToInt64(Math.Pow(2, bitField.Size) - 1))
                {
                    bitField.Value = (BigInteger)Math.Pow(2, bitField.Size) - 1;
                }

                Int64 mask = 1;
                mask = ((mask << bitField.Size) - 1) << bitField.StartBit;
                var newBits = (bitField.Value << bitField.StartBit) & mask;
                var oldBits = CurrentRegisterValue & mask;
                if (newBits != oldBits)
                {
                    CurrentRegisterValue = (CurrentRegisterValue & ~mask) | newBits;
                }
            }
        }

        public BigInteger CurrentRegisterValue
        {
            get
            {
                if (CurrentGroupRegsiters.Count == 0)
                    return 0;
                return CurrentRegister.Value;
            }
            set
            {
                CurrentRegister.Value = value;
                NotifyPropertyChanged(() => CurrentRegisterValue);
                NotifyPropertyChanged(() => CurrentRegisterDisplayValue);
                
                RefreshBFValue();
            }
        }

        public string CurrentRegisterDisplayValue
        {
            get
            {
                if (CurrentGroupRegsiters.Count == 0)
                    return "0";
                return CurrentRegister.DisplayValue;
            }
            set
            {
                CurrentRegister.DisplayValue = value;
                NotifyPropertyChanged(() => CurrentRegisterDisplayValue);
                RefreshBFValue();
            }
        }

        private string mCurrentRegType = "RO";
        public string CurrentRegType
        {
            get { return mCurrentRegType; }
            set
            {
                if (mCurrentRegType != value)
                {
                    mCurrentRegType = value;
                    NotifyPropertyChanged(() => CurrentRegType);
                }
            }
        }

        private string mCurrentRegAddress = "0";
        public string CurrentRegAddress
        {
            get { return mCurrentRegAddress; }
            set
            {
                if (mCurrentRegAddress != value)
                {
                    mCurrentRegAddress = value;
                    NotifyPropertyChanged(() => CurrentRegAddress);
                }
            }
        }

        public ObservableCollection<BitField> CurrentRegsiterBitFields
        {
            get
            {
                return mCurrentBitFields;
            }
            set
            {
                mCurrentBitFields = value;
                NotifyPropertyChanged(() => CurrentRegsiterBitFields);
            }
        }

        private bool mIsDisplayHex = false;
        public bool IsDisplayHex
        {
            get{ return mIsDisplayHex;}
            set {

                if (mIsDisplayHex == value)
                    return;

                mIsDisplayHex = value;

                CurrentRegister.IsDisplayValueHex = mIsDisplayHex;

                foreach (var item in mCurrentBitFields)
                {
                    item.IsDisplayValueHex = mIsDisplayHex;
                }

                NotifyPropertyChanged(() => CurrentRegisterDisplayValue); 
                NotifyPropertyChanged(() => IsDisplayHex);
            }

        }
        internal void RefreshBFValue()
        {
            
            BigInteger value = CurrentRegisterValue;
            foreach (var BF in mCurrentBitFields)
            {
                //BF.Value = ((value << (63-BF.EndBit)) >> (63 - BF.EndBit + BF.StartBit));
                BigInteger bits = value >> BF.StartBit;
                BigInteger mask = 1;
                mask = (mask << BF.Size) - 1;
                bits &= mask;
                BF.Value = bits;

                BF.RefreshDisplay();
            }

            NotifyPropertyChanged(() => CurrentRegsiterBitFields);
        }

        RelayCommand _RegisterWrite;
        public ICommand RegisterWrite
        {
            get { return _RegisterWrite ?? (_RegisterWrite = new RelayCommand(() => DoRegisterWrite())); }
        }
        public void DoRegisterWrite()
        {
            mInstr.Service.WriteRegisterByName(CurrentGroup, CurrentRegister.Name, (Int64)CurrentRegisterValue, true);
            UpdateErrorMessage();
        }

        RelayCommand _RegisterRead;
        public ICommand RegisterRead
        {
            get { return _RegisterRead ?? (_RegisterRead = new RelayCommand(() => DoRegisterRead())); }
        }
        public void DoRegisterRead()
        {

            var valueString = mInstr.Service.ReadRegisterByNameAsString(CurrentGroup, CurrentRegister.Name);
            CurrentRegisterValue = ParseRegValue(valueString, CurrentRegister.Size);
            UpdateErrorMessage();
        }

        private BigInteger ParseRegValue(string valueString,int size)
        {
            try
            {
                BigInteger value = BigInteger.Parse(valueString.Substring(2), NumberStyles.HexNumber);
                BigInteger mask = 1;
                mask = (mask << size) - 1;
                value &= mask;
                return value;
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region Address Access
        public string WriteAddress
        {
            get;
            set;
        }
        public String WriteData
        {
            get;
            set;
        }

        public string WriteFromFilePath
        {
            get;
            set;
        }

        private bool mIsWriteFromFile;

        public bool IsWriteFromFile
        {
            get
            {
                return mIsWriteFromFile;
            }
            set
            {
                mIsWriteFromFile = value;
                // Force refresh of visibility
                NotifyPropertyChanged(() => WriteFromFileVisibility);
                NotifyPropertyChanged(() => WriteFromPanelVisibility);
                NotifyPropertyChanged(() => RegWriteFromPanelVisibility);
                NotifyPropertyChanged(() => MemoryWriteFromPanelVisibility);
            }
        }

        private bool mIsAccessRegister = true;

        public bool IsAccessRegister
        {
            get
            {
                return mIsAccessRegister;
            }
            set
            {
                mIsAccessRegister = value;
                // Force refresh of visibility
                NotifyPropertyChanged(() => WriteFromFileVisibility);
                NotifyPropertyChanged(() => WriteFromPanelVisibility);
                NotifyPropertyChanged(() => RegWriteFromPanelVisibility);
                NotifyPropertyChanged(() => MemoryWriteFromPanelVisibility);
            }
        }

        public Visibility WriteFromFileVisibility
        {
            get
            {
                return IsWriteFromFile ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility WriteFromPanelVisibility
        {
            get
            {
                return !IsWriteFromFile ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        

        public string ReadAddress
        {
            get;
            set;
        }

        public string LengthToRead
        {
            get;
            set;
        }


        public String ReadData
        {
            get;
            set;
        }

        public string ReadToFilePath
        {
            get;
            set;
        }

        private bool mIsReadToFile;
        private string mSelectedAddressType;


        public bool IsReadToFile
        {
            get
            {
                return mIsReadToFile;
            }
            set
            {
                mIsReadToFile = value;
                // Force refresh of visibility
                NotifyPropertyChanged(() => ReadToFileVisibility);
                NotifyPropertyChanged(() => ReadToPanelVisibility);
            }
        }

        private bool mIsInHex = true;

        public bool IsInHex
        {
            get { return mIsInHex; }
            set { mIsInHex = value; }
        }

        public Visibility ReadToFileVisibility
        {
            get
            {
                return IsReadToFile ? Visibility.Visible : Visibility.Collapsed;
            }
        }


        public Visibility ReadVisibility
        {
            get
            {
                return Visibility.Visible;
            }
        }

        public Visibility WriteVisibility
        {
            get
            {
                return Visibility.Visible;
            }
        }

        public Visibility RegWriteFromPanelVisibility
        {
            get
            {
                return (!IsWriteFromFile && IsAccessRegister) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility MemoryWriteFromPanelVisibility
        {
            get
            {
                return (!IsWriteFromFile && !IsAccessRegister) ? Visibility.Visible : Visibility.Collapsed;
            }
        }


        public Visibility ReadToPanelVisibility
        {
            get
            {
                return IsReadToFile ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public List<string> AddressType
        {
            get;
            set;
        }

        public string SelectedAddressType
        {
            get { return mSelectedAddressType; }
            set
            {
                // When switching modules, if the previously selected memory block name
                //  does not exist in the new module, value will be null.  In that case
                //  default to a safe value
                mSelectedAddressType = (value != null) ? value : AddressType[0];

                if (mSelectedAddressType == ADDRESSTYPE_MAPPEDCONTROLREG)
                {
                    IsAccessRegister = true;
                    mMyAddressSpace = MyAddressSpace.MappedControlReg;
                }
                else if (mSelectedAddressType == ADDRESSTYPE_CARRIERREG)
                {
                    IsAccessRegister = true;
                    mMyAddressSpace = MyAddressSpace.CarrierReg;
                }
                else if (mSelectedAddressType == ADDRESSTYPE_SPECTRUMDATAMEMORY)
                {
                    IsAccessRegister = false;
                    mMyAddressSpace = MyAddressSpace.SpectrumDataMemory;
                }
                else
                {
                    throw new Exception("Unsupported Address Space");
                }

            }
        }

        private static string ByteArrayToString(byte[] bytes)
        {
            int count = 0;
            StringBuilder hexStr = new StringBuilder(bytes.Length * 2);
            foreach (byte theByte in bytes)
            {
                hexStr.AppendFormat("{0:x2}", theByte);
                if (count++ >= 7)
                {
                    count = 0;
                    hexStr.Append("\n");
                }
            }
            return hexStr.ToString().ToUpper();
        }

        private static string IntArrayToString(int[] values, bool inHex)
        {
            int count = 0;
            StringBuilder stringValue = new StringBuilder();
            foreach (int value in values)
            {
                if (inHex)
                {
                    stringValue.AppendFormat("0x{0:x},", value);
                }
                else
                {
                    stringValue.AppendFormat("{0},", value);
                }
                if (count++ >= 7)
                {
                    count = 0;
                    stringValue.Append("\n");
                }
            }
            return stringValue.ToString().ToUpper().Remove(stringValue.Length-1);
        }

        private static byte[] HexStringToByteArray(string hexString)
        {
            // Remove '0x' is present (assume it is leading)
            hexString = hexString.Replace("0x", "").Replace(" ", "").Replace("\r", "").Replace("\n", "");

            // Truncation is not divisible by 2
            byte[] hexAsBytes = new byte[hexString.Length / 2];

            for (int i = 0; i < hexAsBytes.Length; i++)
            {
                // Pull off the next 2 characters and convert to hex
                string byteValue = hexString.Substring(i * 2, 2);
                hexAsBytes[i] = byte.Parse(byteValue, NumberStyles.HexNumber);
            }
            return hexAsBytes;
        }


        RelayCommand mWriteBrowse;
        public ICommand WriteBrowse
        {
            get { return mWriteBrowse ?? (mWriteBrowse = new RelayCommand(() => BrowseWriteFromFilePath())); }
        }

        RelayCommand mReadBrowse;
        public ICommand ReadBrowse
        {
            get { return mReadBrowse ?? (mReadBrowse = new RelayCommand(() => BrowseReadToFilePath())); }
        }

        RelayCommand mWriteAddressCommand;
        public ICommand WriteAddressCommand
        {
            get { return mWriteAddressCommand ?? (mWriteAddressCommand = new RelayCommand(() => OnWriteAddress())); }
        }

        RelayCommand mReadAddressCommand;

        public ICommand ReadAddressCommand
        {
            get { return mReadAddressCommand ?? (mReadAddressCommand = new RelayCommand(() => OnReadAddress())); }
        }


        private void OnWriteAddress()
        {
            try
            {

                byte[] bWriteData;
                int[] writeRegVals;

                int writeAddressValue = 0;
                if (!Utility.ConvertStringInt(WriteAddress, ref writeAddressValue))
                {
                    throw new Exception("Invalid address. Enter integer value in decimal <nnn> or hex <0xhhh>");
                }

                if (IsWriteFromFile)
                {
                    bWriteData = File.ReadAllBytes(WriteFromFilePath);
                    //mDriver.WriteMemory(Model, SelectedMemoryBlock, writeAddressValue, bWriteData);
                }
                else
                {

                    if (IsAccessRegister) // Write Register
                    {
                        if (WriteData.Length < 1)
                        {
                            throw new Exception("Empty input");
                        }

                        try
                        {
                            string[] regValStrings = WriteData.Split(',');
                            writeRegVals = new int[regValStrings.Length];
                            for (int i = 0; i < regValStrings.Length; i++)
                            {
                                if (!Utility.ConvertStringInt(regValStrings[i], ref writeRegVals[i]))
                                {
                                    throw new Exception("Invalid value string: " + regValStrings[i]);
                                }
                            }

                            if (writeRegVals.Length == 1)
                            {
                                mInstr.RegDriver.RegWrite((AddressSpace) mMyAddressSpace, writeAddressValue,
                                    writeRegVals[0]);
                            }
                            else
                            {
                                mInstr.RegDriver.ArrayWrite((AddressSpace) mMyAddressSpace, writeAddressValue,
                                    writeRegVals, writeRegVals.Length);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Writing error:" + ex.Message);
                        }

                    }
                    else //Write Memory
                    {
                        if (WriteData.Length < 2)
                        {
                            throw new Exception("Invalid data. Enter at least 1 byte (2 characters)");
                        }

                        try
                        {
                            bWriteData = HexStringToByteArray(WriteData);
                        }
                        catch
                        {
                            throw new Exception("Invalid data. Enter data in hex");
                        }

                        //mDriver.WriteMemory(Model, SelectedMemoryBlock, writeAddressValue, bWriteData);
                    }
                }
            }
            catch (Exception ex)
            {
                mMainViewModel.LatestMessage = ex.Message;

            }
            finally
            {
                UpdateErrorMessage();
            }
        }

        private void OnReadAddress()
        {
            try
            {

                byte[] dataRead = new byte[0];
                int[] regRead = new int[1];

                int readAddressValue = 0;
                if (!Utility.ConvertStringInt(ReadAddress, ref readAddressValue))
                {
                    throw new Exception("Invalid read address. Enter integer value in decimal <nnn> or hex <0xhhh>");
                }

                int byteCount = 0;
                if (!Utility.ConvertStringInt(LengthToRead, ref byteCount) || byteCount < 0)
                {
                    throw new Exception("Invalid 'Number of Bytes' Enter integer value in decimal <nnn> or hex <0xhhh>");
                }

                //mDriver.ReadMemory(Model, SelectedMemoryBlock, readAddressValue, byteCount, out dataRead);
                if (IsReadToFile)
                {
                    if (dataRead != null && dataRead.Length > 0)
                    {
                        FileStream stream = new FileStream(ReadToFilePath,
                            FileMode.OpenOrCreate,
                            FileAccess.Write,
                            FileShare.ReadWrite);
                        stream.Write(dataRead, 0, dataRead.Length);
                        stream.Flush();
                        stream.Close();
                    }
                }
                else
                {
                    if (IsAccessRegister)
                    {
                        if (byteCount == 1)
                        {
                            regRead[0] = mInstr.RegDriver.RegRead((AddressSpace) mMyAddressSpace, readAddressValue);
                        }
                        else
                        {
                            regRead = new int[byteCount];
                            regRead = mInstr.RegDriver.ArrayRead32((AddressSpace)mMyAddressSpace, readAddressValue, byteCount);
                        }

                        ReadData = IntArrayToString(regRead,IsInHex);

                    }
                    else
                    {
                        //TODO, read memory
                        ReadData = ByteArrayToString(dataRead);
                    }


                    NotifyPropertyChanged(() => ReadData);
                }
            }
            finally
            {
                UpdateErrorMessage();
            }
        }

        private void BrowseWriteFromFilePath()
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog
                {
                    Title = "Write from file path",
                    InitialDirectory = Path.GetDirectoryName(WriteFromFilePath),
                    FileName = Path.GetFileName(WriteFromFilePath),
                    DefaultExt = ".bin",
                    Filter = "All Files (*.*)|*.*|Bin File (*.bin)|*.bin",
                    CheckFileExists = true
                };

                // Show open file dialog box - this dialog checks for an existing document.
                Nullable<bool> result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result == true)
                {
                    WriteFromFilePath = dlg.FileName;
                    NotifyPropertyChanged(() => WriteFromFilePath);
                }
            }

            catch (Exception ex)
            {
                //ErrorReporter.ReportError(ex);
            }
        }

        private void BrowseReadToFilePath()
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog
                {
                    Title = "Read to file path",
                    InitialDirectory = Path.GetDirectoryName(ReadToFilePath),
                    FileName = Path.GetFileName(ReadToFilePath),
                    DefaultExt = ".bin",
                    Filter = "All Files (*.*)|*.*|Bin File (*.bin)|*.bin",
                    CheckFileExists = false
                };

                // Show open file dialog box - this dialog checks for an existing document.
                Nullable<bool> result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result == true)
                {
                    ReadToFilePath = dlg.FileName;
                    NotifyPropertyChanged(() => ReadToFilePath);
                }
            }

            catch (Exception ex)
            {
                //ErrorReporter.ReportError(ex);
            }
        }

        #endregion

        public string LatestMessage
        {
            get => mMainViewModel.LatestMessage;
        }

        public void UpdateErrorMessage(bool firstTime = false)
        {
            mMainViewModel.UpdateErrorMessage(firstTime);
            NotifyPropertyChanged(()=> LatestMessage);
        }
        //{
        //    get => mLatestMessage;
        //    set
        //    {
        //        mLatestMessage = value;
        //        NotifyPropertyChanged((() => LatestMessage));
        //    }
        //}

        //#region ErrorMessage

        //private string mLatestMessage = "---";

        //public string LatestMessage
        //{
        //    get => mLatestMessage;
        //    set
        //    {
        //        mLatestMessage = value;
        //        NotifyPropertyChanged((() => LatestMessage));
        //    }
        //}

        //private int mLastErrorConut = 0;

        //public void UpdateErrorMessage(bool firstTime = false)
        //{
        //    if (firstTime)
        //    {
        //        mLastErrorConut = mInstr.ErrorLog.ErrorList.Count;
        //        mLatestMessage = "";
        //    }
        //    else
        //    {
        //        int currentCount = mInstr.ErrorLog.ErrorList.Count;
        //        if (currentCount == mLastErrorConut)
        //        {
        //            LatestMessage = "";
        //        }
        //        else
        //        {
        //            if (mInstr.ErrorLog.ErrorList.Count > 0)
        //            {
        //                LatestMessage = mInstr.ErrorLog.ErrorList.Last().Message;
        //                mLastErrorConut = currentCount;
        //            }
        //            else
        //            {
        //                LatestMessage = "";
        //                mLastErrorConut = 0;
        //            }
        //        }
        //    }
        //}


        //#endregion
    }

    public class Register
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int Size { get; set; }
        public Int64 Address { get; set; }
        public BigInteger Value { get; set; }
        public bool IsDisplayValueHex { get; set; } = false;
        public string DisplayValue
        {
            get
            {
                return FormatBigIntegerForDisplay(Value);
            }
            set
            {
                try
                {
                    const string hexPattern = "^0x";
                    const RegexOptions options = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;
                    bool isHex = IsDisplayValueHex || Regex.IsMatch(value, hexPattern, options);
                    if (isHex)
                    {
                        if (!Regex.IsMatch(value, hexPattern, options))
                        {
                            value = "0x" + value;
                        }

                        // Note: Must prepend a '0' when parsing a big integer as hex - otherwise, any leading 
                        //  hex digit with the MSB set will cause the value to be interpreted as negative
                        string hexValue = Regex.Replace(value, hexPattern, "0", options);
                        Value = BigInteger.Parse(hexValue, NumberStyles.HexNumber);
                    }
                    else
                    {
                        Value = BigInteger.Parse(value);
                    }
                }
                catch (Exception)
                {
                    // ignore
                }

            }
        }
        private string FormatBigIntegerForDisplay(BigInteger val)
        {
            string strVal;
            if (IsDisplayValueHex)
            {
                strVal = val.ToString("x");
                if (strVal.Length > 1 && strVal.StartsWith("0"))
                {
                    strVal = strVal.Substring(1);
                }

                strVal = "0x" + strVal;
            }
            else
            {
                strVal = val.ToString();
            }

            return strVal;
        }
    }

    public class BitField : AbstractModel
    {
        public string Name { get; set; }
        public int StartBit { get; set; }
        public int EndBit { get; set; }
        public int Size { get; set; }

        private BigInteger mValue = 0;
        public BigInteger Value
        {
            get
            {
                return mValue;
            }
            set
            {
                if (value != mValue)
                {
                    mValue = value;
                    NotifyPropertyChanged(() => Value);
                }
            }
        }

        private bool mIsDisplayValueHex = false;
        public bool IsDisplayValueHex
        {
            get
            {
                return mIsDisplayValueHex;
            }
            set
            {
                if (mIsDisplayValueHex != value)
                {
                    mIsDisplayValueHex = value;
                    NotifyPropertyChanged(() => DisplayValue);
                }
            }
        }

        /// <summary>
        /// The displayable value of this field (normally just value formatted as hexadecimal)
        /// </summary>
        public string DisplayValue
        {
            get
            {
                return FormatBigIntegerForDisplay(Value);
            }
            set
            {
                try
                {
                    const string hexPattern = "^0x";
                    const RegexOptions options = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;
                    bool isHex = IsDisplayValueHex || Regex.IsMatch(value, hexPattern, options);
                    if (isHex)
                    {
                        if (!Regex.IsMatch(value, hexPattern, options))
                        {
                            value = "0x" + value;
                        }

                        // Note: Must prepend a '0' when parsing a big integer as hex - otherwise, any leading 
                        //  hex digit with the MSB set will cause the value to be interpreted as negative
                        string hexValue = Regex.Replace(value, hexPattern, "0", options);
                        Value = BigInteger.Parse(hexValue, NumberStyles.HexNumber);
                    }
                    else
                    {
                        Value = BigInteger.Parse(value);
                    }
                }
                catch (Exception)
                {
                    // ignore
                }

                NotifyPropertyChanged(() => DisplayValue);
            }

        }

        private string FormatBigIntegerForDisplay(BigInteger val)
        {
            string strVal;
            if (IsDisplayValueHex)
            {
                strVal = val.ToString("x");
                if (strVal.Length > 1 && strVal.StartsWith("0"))
                {
                    strVal = strVal.Substring(1);
                }
                if (Size > 1)
                {
                    strVal = "0x" + strVal;
                }
            }
            else
            {
                strVal = val.ToString();
            }

            return strVal;
        }


        public void RefreshDisplay()
        {
            NotifyPropertyChanged(() => DisplayValue);
        }
    }

    public class UInt64StringFormatter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (IsInt(value as string))
                {
                    return System.Convert.ToUInt64(value);
                }
                else
                { return 0; }
            }
            catch
            {
                return 0;
            }
        }

        public static bool IsInt(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            return Regex.IsMatch(value, @"^[+-]?\d*$");
        }
    }

    public class RegTypeToWritableFlag : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string valueStr = value as string;
            switch (valueStr)
            {
                case "RO":
                    return false;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

    public class RegTypeToReadableFlag : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string valueStr = value as string;
            switch (valueStr)
            {
                case "WO":
                    return false;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
