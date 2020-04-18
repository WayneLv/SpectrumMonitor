using InstrumentDriver.Core.Utility;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SpectrumMonitor.ViewModel
{
    public enum eMarkerState
    {
        Off = 0,
        Normal,
        Delta,
    }

    public enum eMarkerType
    {
        Spectrum = 0,
        Spetrogram
    }

    public struct Range<T>
    {
        public T Start;
        public T End;
        public T Size;
    }

    public class Marker: AbstractModel
    {
        //three dimensional Marker, X for Frequency, Y for Power value, Z for Time(Spectrogram only)


        double mXValue = 0;
        double mYValue = 0;
        double mZValue = 0;
        double mXRefValue = 0;
        double mYRefValue = 0;
        double mZRefValue = 0;

        eMarkerState mState = eMarkerState.Off;
        private bool mRefSetDone = false;

        private SpctrumMonitorViewModel mMainViewModel;

        public Marker(SpctrumMonitorViewModel mainViewModel)
        {
            mMainViewModel = mainViewModel;
        }

        public eMarkerType Type { get; set; } = eMarkerType.Spectrum;


        public int Index { get; set; } = 0;
        public int XPos { get; set; } = 0;
        public int YPos { get; set; } = 0;

        private int mZPos = 0;
        public int ZPos
        {
            get { return mZPos; }
            set
            {
                if (mZPos == value)
                    return;

                mZPos = value;
                NotifyPropertyChanged(() => ZPos);
            }
        }
        public int XRefPos { get; set; } = 0;
        public int YRefPos { get; set; } = 0;
        public int ZRefPos { get; set; } = 0;
        public double XIndexScale { get; set; } = 0.3;

       
        public Color Color { get; set; } = Colors.White;

        public Range<double> XValueRange = new Range<double>();
        public Range<double> YValueRange = new Range<double>();
        public Range<double> ZValueRange = new Range<double>();

        public Range<int> XPosRange = new Range<int>();
        public Range<int> YPosRange = new Range<int>();
        public Range<int> ZPosRange = new Range<int>();

        public double XValue
        {
            get { return mXValue; }
            set
            {
                if(mXValue == value)
                {
                    return;
                }

                value = Math.Max(XValueRange.Start, Math.Min(value, XValueRange.End));

                mXValue = value;

                UpdateXPos();

                XIndexScale = (mXValue - XValueRange.Start) / XValueRange.Size;

                NotifyPropertyChanged(() => XValue);
                NotifyPropertyChanged(() => XValueString);

                double[] data = new double[0];

                if (Type == eMarkerType.Spectrum)
                {
                    data = mMainViewModel.SpectrumAreaViewModel.LastSpectrumData;
                }
                else if (Type == eMarkerType.Spetrogram)
                {
                    if (mMainViewModel.SpectrogramAreaViewModel.SpectrogramData.Count == 0)
                        return;

                    int zIndex = mMainViewModel.SpectrogramAreaViewModel.SpectrogramData.Count - 1 - ZPos;
                    if (zIndex < 0)
                        zIndex = 0;

                    Time = mMainViewModel.SpectrogramAreaViewModel.SpectrogramData[zIndex].Key;

                    ZValue = (double)(Time - mMainViewModel.SpectrogramAreaViewModel.StartTime).TotalSeconds;

                    data = mMainViewModel.SpectrogramAreaViewModel.SpectrogramData[zIndex].Value;
                }

                int dataIndex = (int)Math.Max(0, Math.Min(XIndexScale * data.Length, data.Length-1));

                if (data.Length > 0)
                {
                    YValue = data[dataIndex];
                }

            }
        }

        public double YValue
        {
            get { return mYValue; }
            set
            {
                if (mYValue == value)
                {
                    return;
                }
                mYValue = value;

                UpdateYPos();

                if (State == eMarkerState.Delta && !mRefSetDone)
                {
                    //Ref Value Not set, set it one time
                    SetCurrentValuesAsRef();
                    mRefSetDone = true;
                }

                NotifyPropertyChanged(() => YValue);
                NotifyPropertyChanged(() => YValueString);
            }
        }

        public double ZValue
        {
            get { return mZValue; }
            set
            {
                if (mZValue == value)
                {
                    return;
                }
                mZValue = value;
                NotifyPropertyChanged(() => ZValue);
                NotifyPropertyChanged(() => ZValueString);
            }
        }

        public double XRefValue
        {
            get { return mXRefValue; }
            set
            {
                if (mXRefValue == value)
                {
                    return;
                }
                mXRefValue = value;
                NotifyPropertyChanged(() => XRefValue);
            }
        }

        public double YRefValue
        {
            get { return mYRefValue; }
            set
            {
                if (mYRefValue == value)
                {
                    return;
                }
                mYRefValue = value;
                NotifyPropertyChanged(() => YRefValue);
            }
        }

        public double ZRefValue
        {
            get { return mZRefValue; }
            set
            {
                if (mZRefValue == value)
                {
                    return;
                }
                mZRefValue = value;
                NotifyPropertyChanged(() => ZRefValue);
            }
        }


        public eMarkerState State
        {
            get { return mState; }
            set
            {
                if (mState == value)
                    return;

                mRefSetDone = false;

                switch (value)
                {
                    case eMarkerState.Off:
                        MarkerVisibility = Visibility.Hidden;
                        RefVisibility = Visibility.Hidden;
                        break;
                    case eMarkerState.Normal:
                        MarkerVisibility = Visibility.Visible;
                        RefVisibility = Visibility.Hidden;
                        break;
                    case eMarkerState.Delta:
                        //Already Marker on, set value to Reference
                        SetCurrentValuesAsRef();
                        if (mState == eMarkerState.Normal)
                        {
                            mRefSetDone = true;
                        }
                        MarkerVisibility = Visibility.Visible;
                        RefVisibility = Visibility.Visible;

                        break;
                    default:
                        break;
                }

                mState = value;
                NotifyPropertyChanged(() => State);
                NotifyPropertyChanged(() => Visible);
                NotifyPropertyChanged(() => LabelName);
                NotifyPropertyChanged(() => XValueString);
                NotifyPropertyChanged(() => YValueString);

                NotifyPropertyChanged(() => MarkerVisibility);
                NotifyPropertyChanged(() => RefVisibility);
            }
        }

        public void UpdateXValueRange(double start, double end)
        {
            XValueRange.Start = start;
            XValueRange.End = end;
            XValueRange.Size = end - start;
        }

        public void UpdateYValueRange(double start, double end)
        {
            YValueRange.Start = start;
            YValueRange.End = end;
            YValueRange.Size = end - start;
        }

        public void UpdateZValueRange(double start, double end)
        {
            ZValueRange.Start = start;
            ZValueRange.End = end;
            ZValueRange.Size = end - start;
        }

        public void UpdateXPosRange(int start, int end)
        {
            XPosRange.Start = start;
            XPosRange.End = end;
            XPosRange.Size = end - start;
        }

        public void UpdateYPosRange(int start, int end)
        {
            YPosRange.Start = start;
            YPosRange.End = end;
            YPosRange.Size = end - start;
        }

        public void UpdateZPosRange(int start, int end)
        {
            ZPosRange.Start = start;
            ZPosRange.End = end;
            ZPosRange.Size = end - start;
        }

        public void UpdateXPos()
        {
            XPos = XPosRange.Start + (int)Math.Round((XValue - XValueRange.Start) / XValueRange.Size * XPosRange.Size);
            NotifyPropertyChanged(() => XPos);
        }

        public void UpdateYPos()
        {
            int pos = YPosRange.Start + (int)Math.Round((YValue - YValueRange.Start) / YValueRange.Size * YPosRange.Size);
            YPos = Math.Max(YPosRange.Start, Math.Min(YPosRange.End, pos));

            NotifyPropertyChanged(() => YPos);
        }

        public void UpdateZPos()
        {
            ZPos = ZPosRange.Start + (int)Math.Round((ZValue - ZValueRange.Start) / ZValueRange.Size * ZPosRange.Size);
            NotifyPropertyChanged(() => ZPos);
        }



        public void SetCurrentValuesAsRef()
        {
            XRefValue = XValue;
            YRefValue = YValue;
            ZRefValue = ZValue;

            XRefPos = XPos;
            YRefPos = YPos;
            ZRefPos = ZPos;

            NotifyPropertyChanged(() => XRefPos);
            NotifyPropertyChanged(() => YRefPos);
            NotifyPropertyChanged(() => ZRefPos);
        }

        public Visibility Visible
        {
            get
            {
                switch (mState)
                {
                    case eMarkerState.Normal:
                    case eMarkerState.Delta:
                        return Visibility.Visible;
                    case eMarkerState.Off:
                    default:
                        return Visibility.Collapsed;
                }
            }
        }

        public string LabelName
        {
            get
            {
                string label = "";
                if (mState == eMarkerState.Normal)
                {
                    label = string.Format("M{0}:",Index+1);
                }
                else if (mState == eMarkerState.Delta)
                {
                    label = string.Format("D{0}:", Index+1);
                }
                return label;
            }
        }
        public string YValueString
        {
            get
            {
                string value = "";
                if (mState == eMarkerState.Normal)
                {
                    value = string.Format("{0:F2}dBm", YValue);
                }
                else if (mState == eMarkerState.Delta)
                {
                    value = string.Format("{0:F2}dB", YValue - YRefValue);
                }
                return value;
            }
        }

        private string mXValueString;
        public string XValueString
        {
            get
            {
                if (mState == eMarkerState.Normal)
                {
                    mXValueString = Utility.FrequencyValueToString(XValue);
                }
                else if (mState == eMarkerState.Delta)
                {
                    mXValueString = Utility.FrequencyValueToString(XValue-XRefValue);
                }
                return mXValueString;
            }
            set
            {
                if (mXValueString == value)
                    return;

                double newvalue = Utility.FrequencyStringToValue(value);
                if (mState == eMarkerState.Normal)
                {
                    XValue = (double)newvalue;
                }
                else if (mState == eMarkerState.Delta)
                {
                    XValue = XRefValue + (double)newvalue;
                }

            }
        }

        public string ZValueString
        {
            get
            {
                return ZValue.ToString("F3")+"s";
            }
        }

        private DateTime mTime;
        public DateTime Time
        {
            get
            {
                return mTime;
            }
            set
            {
                mTime = value;
                NotifyPropertyChanged(() => Time);
                NotifyPropertyChanged(() => TimeString);
            }
        }
        public string TimeString
        {
            get
            {
                return Time.ToLongTimeString();
            }
        }


        private double mIconSizeScale = 1.0;
        public double SizeScale
        {
            get { return mIconSizeScale; }
            set
            {
                mIconSizeScale = value;
                NotifyPropertyChanged(() => SizeScale);
            }
        }
        public string IconLabel
        {
            get
            {
                switch (State)
                {
                    
                    case eMarkerState.Normal:
                        return "M" + (Index + 1).ToString();
                    case eMarkerState.Delta:
                        return "D" + (Index + 1).ToString();
                    case eMarkerState.Off:
                    default:
                        return "";
                }
            }
        }
        public string RefIconLabel
        {
            get
            {
                switch (State)
                {
                    case eMarkerState.Delta:
                        return "R" + (Index + 1).ToString();
                    case eMarkerState.Normal:
                    case eMarkerState.Off:
                    default:
                        return "";
                }
            }
        }
        public Color IconColor { get; set; } = Colors.White;
        public Visibility MarkerVisibility { get; set; } = Visibility.Hidden;
        public Visibility RefVisibility { get; set; } = Visibility.Hidden;
        

    }

    public class MarkerStateEnumToIndex : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (eMarkerState)Enum.ToObject(typeof(eMarkerState), value);
        }

    }
}
