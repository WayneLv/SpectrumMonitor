using InstrumentDriver.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Input;

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

        public RegisterControlViewModel(SpctrumMonitorViewModel mainViewModel)
        {
            mInstr = mainViewModel.Instrument;

            var groupstr = mInstr.Service.GetRegisterGroups();

            string[] groups = groupstr.Split(',');
            mRegisterGroupItems.Clear();
            mRegisterGroupItems.AddRange(groups);

        }


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
