﻿using InstrumentDriver.Core.Interfaces;
using InstrumentDriver.Core.Utility;
using InstrumentDriver.SpectrumMonitor;
using SpectrumMonitor.Controls;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
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


        internal SpctrumMonitorViewModel()
        {
            mInstr = new SpectrumMonitorInstrument(true);

            mRegisterControlViewModel = new RegisterControlViewModel(this);
            mIndicatorViewModel = new IndicatorViewModel(this);
            mFunctionViewModel = new FunctionViewModel(this);
            mMenuViewModel = new MenuViewModel(this);
            mSettingPanelViewModel = new SettingPanelViewModel(this);
            mSpectrumAreaViewModel = new SpectrumAreaViewModel(this);
            mSpectrogramAreaViewModel = new SpectrogramAreaViewModel(this);
            mSignalTableAreaViewModel = new ViewModel.SignalTableAreaViewModel(this);
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
        #endregion

        #region NotifyProperty
        private string mLatestMessage = " No Error";
        public string LatestMessage
        {
            set
            {
                mLatestMessage = value;
                NotifyPropertyChanged(() => LatestMessage);
            }
            get
            {
                return mLatestMessage;
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
        #endregion

        public Boolean? PowerState { get; set; } = true;
        public Boolean? ErrorState { get; set; } = false;
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