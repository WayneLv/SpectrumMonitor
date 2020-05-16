using InstrumentDriver.Core.Interfaces;
using InstrumentDriver.Core.Utility;
using InstrumentDriver.SpectrumMonitor;
using SpectrumMonitor.Controls;
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SpectrumMonitor.ViewModel
{
    public class SpctrumMonitorViewModel : AbstractModel
    {
        private SpectrumMonitorInstrument mInstr;

        private readonly RegisterControlViewModel mRegisterControlViewModel;
        private readonly IndicatorViewModel mIndicatorViewModel;
        private readonly FunctionViewModel mFunctionViewModel;
        private readonly MenuViewModel mMenuViewModel;
        private readonly SettingPanelViewModel mSettingPanelViewModel;
        private readonly SpectrogramAreaViewModel mSpectrogramAreaViewModel;
        private readonly SpectrumAreaViewModel mSpectrumAreaViewModel;
        private readonly SignalTableAreaViewModel mSignalTableAreaViewModel;
        private readonly DpxDisplayViewModel mDpxDisplayViewModel;
        private readonly ErrorMessageViewModel mErrorMessageViewModel;
        private readonly DeviceInfoViewModel mDeviceInfoViewModel;


        internal SpctrumMonitorViewModel()
        {
            mInstr = new SpectrumMonitorInstrument(Configuration.Simulation,Configuration.DriverOption);

            mRegisterControlViewModel = new RegisterControlViewModel(this);
            mIndicatorViewModel = new IndicatorViewModel(this);
            mFunctionViewModel = new FunctionViewModel(this);
            mMenuViewModel = new MenuViewModel(this);
            mSettingPanelViewModel = new SettingPanelViewModel(this);
            mSpectrumAreaViewModel = new SpectrumAreaViewModel(this);
            mSpectrogramAreaViewModel = new SpectrogramAreaViewModel(this);
            mDpxDisplayViewModel = new DpxDisplayViewModel(this);
            mSignalTableAreaViewModel = new ViewModel.SignalTableAreaViewModel(this);
            mErrorMessageViewModel = new ErrorMessageViewModel(this);
            mDeviceInfoViewModel = new DeviceInfoViewModel(this);
        }

        #region Private



        #endregion

        #region Property
        public IInstrument Instrument
        {
           get { return mInstr; }
        }

        #endregion

        #region Windows And Controls


        public MainWindow MainWindowInstance { get; set; }
        public IndicatorAreaControl IndicatorAreaControl { get; set; }
        public FunctionAreaControl FunctionAreaControl { get; set; }
        public MenuAreaControl MenuAreaControl { get; set; }
        public SpectrumAreaControl SpectrumAreaControl { get; set; }
        public SpectrogramAreaControl SpectrogramAreaControl { get; set; }
        public SignalTableAreaControl SignalTableAreaControl { get; set; }

        public DPXDisplayControl DPXDisplayControl { get; set; }
        #endregion

        #region ErrorMessage

        private string mLatestMessage = "";

        public string LatestMessage
        {
            get => mLatestMessage;
            set
            {
                mLatestMessage = value;
                NotifyPropertyChanged((() => LatestMessage));
            }
        }

        private int mLastErrorConut = 0;
        private bool mDpxDisplayEnalbed = true;

        public void UpdateErrorMessage(bool firstTime = false)
        {
            if (firstTime)
            {
                mLastErrorConut = mInstr.ErrorLog.ErrorList.Count;
                mLatestMessage = "";
            }
            else
            {
                int currentCount = mInstr.ErrorLog.ErrorList.Count;
                if (currentCount == mLastErrorConut)
                {
                    LatestMessage = "";
                }
                else
                {
                    if (mInstr.ErrorLog.ErrorList.Count > 0)
                    {
                        LatestMessage = mInstr.ErrorLog.ErrorList.Last().Message;
                        mLastErrorConut = currentCount;
                    }
                    else
                    {
                        LatestMessage = "";
                        mLastErrorConut = 0;
                    }
                }
            }
        }


        #endregion


        #region Command


        #endregion

        #region ViewModels

        internal RegisterControlViewModel RegisterControlViewModel
        {
            get { return mRegisterControlViewModel; }
        }

        internal IndicatorViewModel IndicatorViewModel
        {
            get { return mIndicatorViewModel; }
        }

        internal FunctionViewModel FunctionViewModel
        {
            get { return mFunctionViewModel; }
        }

        internal MenuViewModel MenuViewModel
        {
            get { return mMenuViewModel; }
        }

        internal SettingPanelViewModel SettingPanelViewModel
        {
            get { return mSettingPanelViewModel; }
        }

        internal SpectrogramAreaViewModel SpectrogramAreaViewModel
        {
            get { return mSpectrogramAreaViewModel; }
        }

        internal SpectrumAreaViewModel SpectrumAreaViewModel
        {
            get { return mSpectrumAreaViewModel; }
        }

        internal SignalTableAreaViewModel SignalTableAreaViewModel
        {
            get { return mSignalTableAreaViewModel; }
        }

        internal ErrorMessageViewModel ErrorMessageViewModel
        {
            get { return mErrorMessageViewModel; }
        }

        internal DeviceInfoViewModel DeviceInfoViewModel => mDeviceInfoViewModel;

        internal DpxDisplayViewModel DpxDisplayViewModel => mDpxDisplayViewModel;

        #endregion

        public Boolean? PowerState { get; set; } = true;
        public Boolean? ErrorState { get; set; } = false;


        public bool DpxDisplayEnalbed
        {
            set
            {
                mDpxDisplayEnalbed = value;
                NotifyPropertyChanged(()=> SpectrumDisplayVisibility);
                NotifyPropertyChanged(() => DpxDisplayVisibility);
            }
            get => mDpxDisplayEnalbed;
        }

        public Visibility SpectrumDisplayVisibility
        {
            get
            {
                return DpxDisplayEnalbed ? Visibility.Hidden : Visibility.Visible;
            }
        }

        public Visibility DpxDisplayVisibility
        {
            get
            {
                return DpxDisplayEnalbed ? Visibility.Visible : Visibility.Hidden;
            }
        }

    }

    
    public class DoubleToStringDisplay : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return String.Format("{0:F2}", value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {

            string valuestr = value as string;
            valuestr = Regex.Replace(valuestr, @"[^\d.-]", "");
            double floatvalue = 0;
            if (double.TryParse(valuestr, out floatvalue))
            {

            }
            return floatvalue;
        }

    }

    public class OnOffStateToImage : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as Boolean?).GetValueOrDefault() ? "../Images/On.png" : "../Images/Off.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

    public class DoubleToFreqDisplay : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double dvalue = double.Parse(value.ToString());

            return Utility.FrequencyValueToString(dvalue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {

            return  (double)Utility.FrequencyStringToValue(value as string);
        }

    }

    public class ColorToSolidBrush : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new SolidColorBrush((Color)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {

            throw new NotImplementedException();
        }

    }
}
