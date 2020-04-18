using InstrumentDriver.SpectrumMonitor;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace SpectrumMonitor.ViewModel
{

    public enum eTraceMode
    {
        ClearWrite = 0,
        MaxHold,
        MinHold,
    }



    public class SpectrumAreaViewModel : AbstractModel
    {
        public static readonly int MARKER_NUM = 4;

        SpectrumMonitorInstrument mInstr;
        SpctrumMonitorViewModel mMainViewModel;

        private eTraceMode mTraceMode = eTraceMode.ClearWrite;//Clear Write = 0, Max Hold = 1, Min Hold = 2
        private readonly List<string> mTraceModeItems = new List<string> { "Clear Write", "Max Hold", "Min Hold" };

        private readonly List<string> mDetectorTypeItems = new List<string> { "Peak", "Neg Peak", "Sample", "Average" };


        private int mCurrentMarker = 0;
        private readonly List<string> mMarkerItems = new List<string>();
        private readonly List<string> mMarkerStateItems = new List<string> { "Off", "Normal", "Delta" };


        private double mTopLevel = 0.0;
        private double mBottomLevel = -100.0;

        public ObservableCollection<Marker> mDisplayMarkers = new ObservableCollection<Marker>();

        public SpectrumAreaViewModel(SpctrumMonitorViewModel mainviewmodel)
        {
            mInstr = mainviewmodel.Instrument as SpectrumMonitorInstrument;
            mMainViewModel = mainviewmodel;

            for (int i = 0; i < MARKER_NUM; i++)
            {
                Markers[i] = new Marker(mainviewmodel);
                Markers[i].Index = i;

                mMarkerItems.Add("Marker" + (i+1).ToString());
            }
        }

        public double[] LastSpectrumData { get; set; } = new double[0];
        public double[] MaxHoldSpectrumData { get; set; }
        public double[] MinHoldSpectrumData { get; set; }
        public double[] AveragedSpectrumData { get; set; }

        public Marker[] Markers { get; } = new Marker[MARKER_NUM];

        public eTraceMode TraceMode
        {
            get { return mTraceMode; }
            set
            {
                if (mTraceMode == value)
                {
                    return;
                }
                mTraceMode = value;
                switch (value)
                {
                    case eTraceMode.ClearWrite:
                        break;
                    case eTraceMode.MaxHold:
                        if (MaxHoldSpectrumData == null || MaxHoldSpectrumData.Length != LastSpectrumData.Length)
                        {
                            MaxHoldSpectrumData = new double[LastSpectrumData.Length];
                        }
                        for (int i = 0; i < MaxHoldSpectrumData.Length; i++)
                        {
                            MaxHoldSpectrumData[i] = -9999.0;
                        }
                        break;
                    case eTraceMode.MinHold:
                        if (MinHoldSpectrumData == null || MinHoldSpectrumData.Length != LastSpectrumData.Length)
                        {
                            MinHoldSpectrumData = new double[LastSpectrumData.Length];
                        }
                        for (int i = 0; i < MinHoldSpectrumData.Length; i++)
                        {
                            MinHoldSpectrumData[i] = 9999.0;
                        }
                        break;
                    default:
                        break;
                }

                //Clear Average counting
                AverageCounting = 0;

                NotifyPropertyChanged(() => TraceMode);
            }
        }
        public List<string> TraceModeItems
        {
            get { return mTraceModeItems; }
        }

        public List<string> DetectorTypeItems
        {
            get { return mDetectorTypeItems; }
        }

        public int CurrentMarker
        {
            get { return mCurrentMarker; }
            set
            {
                mCurrentMarker = value;
                NotifyPropertyChanged(() => CurrentMarker);
                NotifyPropertyChanged(() => CurrentMarkerState);
                NotifyPropertyChanged(() => CurrentMarkerVisibility);
            }
        }
        public List<string> MarkerItems
        {
            get { return mMarkerItems; }
        }

        public List<string> MarkerStateItems
        {
            get { return mMarkerStateItems; }
        }

        public eMarkerState CurrentMarkerState
        {
            get { return Markers[CurrentMarker].State; }
            set
            {
                if (Markers[CurrentMarker].State == value)
                    return;

                Markers[CurrentMarker].State = value;

                NotifyPropertyChanged(() => CurrentMarkerState);
                NotifyPropertyChanged(() => CurrentMarkerVisibility);

                //Update display Marker
                DisplayMarkers.Clear();
                for (int i = 0; i < MARKER_NUM; i++)
                {
                    if (Markers[i].State != eMarkerState.Off)
                    {
                        DisplayMarkers.Add(Markers[i]);
                    }
                }
                NotifyPropertyChanged(() => DisplayMarkers);

            }
        }

        public ObservableCollection<Marker> DisplayMarkers
        {
            get
            {
                return mDisplayMarkers;
            }
            set
            {
                mDisplayMarkers = value;
                NotifyPropertyChanged(() => DisplayMarkers);
            }
        }

        public double TopLevel
        {
            get { return mTopLevel; }
            set {
                if (value > 100)
                {
                    value = 100;
                }
                else if (value < BottomLevel + 0.1)
                {
                    value = BottomLevel + 0.1;
                }

                if (mTopLevel != value)
                {
                    mTopLevel = value;
                    NotifyPropertyChanged(() => TopLevel);
                    NotifyPropertyChanged(() => DBPerDiv);

                    mMainViewModel.SpectrogramAreaViewModel.NotifyColorBarChange();

                    UpdateMarkerYRange();
                }
            }
        }

        public double BottomLevel
        {
            get { return mBottomLevel; }
            set
            {
                if (value < -200)
                {
                    value = -200;
                }
                else if (value > TopLevel - 0.1)
                {
                    value = TopLevel - 0.1;
                }

                if (mBottomLevel != value)
                {
                    mBottomLevel = value;
                    NotifyPropertyChanged(() => BottomLevel);
                    NotifyPropertyChanged(() => DBPerDiv);

                    mMainViewModel.SpectrogramAreaViewModel.NotifyColorBarChange();

                    UpdateMarkerYRange();
                }
            }
            
        }

        public double StartFrequency
        {
            get { return mInstr.StartFrequency; }
            set
            { 
                if (mInstr.StartFrequency != value)
                {
                    mInstr.StartFrequency = value;
                    mInstr.Apply();

                    NotifyPropertyChanged(() => StartFrequency);
                    NotifyPropertyChanged(() => Span);
                    NotifyPropertyChanged(() => Center);

                    UpdateMarkerXRange();
                }
            }
        }

        public double StopFrequency
        {
            get { return mInstr.StopFrequency; }
            set
            {
                if (mInstr.StopFrequency != value)
                {
                    mInstr.StopFrequency = value;
                    mInstr.Apply();

                    NotifyPropertyChanged(() => StopFrequency);
                    NotifyPropertyChanged(() => Span);
                    NotifyPropertyChanged(() => Center);

                    UpdateMarkerXRange();
                }
            }
        }

        public double Span
        {
            get { return StopFrequency - StartFrequency; }
        }

        public double Center
        {
            get { return (StopFrequency + StartFrequency)/2; }
        }

        public double RBW
        {
            get { return mInstr.RBW; }
            set
            {
                if (mInstr.RBW != value)
                {
                    mInstr.RBW = value;
                    mInstr.Apply();

                    NotifyPropertyChanged(() => RBW);

                    UpdateMarkerXRange();
                }
            }
        }


        public eDetectorType DetectorType
        {
            get { return mInstr.DisplayDetectorType; }
            set
            {
                if (mInstr.DisplayDetectorType == value)
                    return;

                mInstr.DisplayDetectorType = value;
                mInstr.Apply();

                NotifyPropertyChanged(()=>DetectorType);
            }

        }

        public double DBPerDiv
        {
            get { return (mTopLevel - mBottomLevel)/10; }
        }

        private Boolean? mAverageState = false;
        public Boolean? AverageState
        {
            get { return mAverageState; }
            set
            {
                mAverageState = value;
                if (value.GetValueOrDefault())
                {
                    AvgCountingVisibility = Visibility.Visible;
                    if (AveragedSpectrumData == null || AveragedSpectrumData.Length != LastSpectrumData.Length)
                    {
                        AveragedSpectrumData = new double[LastSpectrumData.Length];
                    }
                    for (int i = 0; i < AveragedSpectrumData.Length; i++)
                    {
                        AveragedSpectrumData[i] = 0;
                    }
                }
                else
                {
                    AverageCounting = 0;
                    AvgCountingVisibility = Visibility.Hidden;
                }

                NotifyPropertyChanged(() => AverageState);
            }
        }

        private UInt32 mAverageNumber = 100;
        public UInt32 AverageNumber
        {
            get { return mAverageNumber; }
            set
            {
                if (mAverageNumber == value)
                    return;
                mAverageNumber = value;
                //Clear Average counting
                AverageCounting = 0;

                NotifyPropertyChanged(() => AverageNumber);
            }
        }

        private UInt32 mAverageCounting = 0;
        public UInt32 AverageCounting
        {
            get { return mAverageCounting; }
            set
            {
                if (value > mAverageNumber)
                    return;

                mAverageCounting = value;
                NotifyPropertyChanged(() => AverageCounting);
            }
        }

        private Visibility mAvgCountingVisibility = Visibility.Hidden;
        public Visibility AvgCountingVisibility
        {
            get { return mAvgCountingVisibility; }
            set
            {
                if (value == mAvgCountingVisibility) return;
                mAvgCountingVisibility = value;
                NotifyPropertyChanged(() => AvgCountingVisibility);
            }
        }

        public void UpdateData(double[] spectrum)
        {

            LastSpectrumData = spectrum;

            UpdateDataForTraceMode();

            UpdateDataForAverage();

            UpdateMarkerData();
        }

        private void UpdateDataForTraceMode()
        {
            //Trace Mode Update
            switch (TraceMode)
            {
                case eTraceMode.ClearWrite:
                    break;
                case eTraceMode.MaxHold:
                    if (MaxHoldSpectrumData.Length != LastSpectrumData.Length)
                    {
                        MaxHoldSpectrumData = new double[LastSpectrumData.Length];
                        for (int i = 0; i < MaxHoldSpectrumData.Length; i++)
                        {
                            MaxHoldSpectrumData[i] = -9999.0;
                        }
                    }
                    for (int i = 0; i < MaxHoldSpectrumData.Length; i++)
                    {
                        if (LastSpectrumData[i] > MaxHoldSpectrumData[i])
                        {
                            MaxHoldSpectrumData[i] = LastSpectrumData[i];
                        }
                    }
                    LastSpectrumData = MaxHoldSpectrumData;
                    break;
                case eTraceMode.MinHold:
                    if (MinHoldSpectrumData.Length != LastSpectrumData.Length)
                    {
                        MinHoldSpectrumData = new double[LastSpectrumData.Length];
                        for (int i = 0; i < MinHoldSpectrumData.Length; i++)
                        {
                            MinHoldSpectrumData[i] = 9999.0;
                        }
                    }
                    for (int i = 0; i < MinHoldSpectrumData.Length; i++)
                    {
                        if (LastSpectrumData[i] < MinHoldSpectrumData[i])
                        {
                            MinHoldSpectrumData[i] = LastSpectrumData[i];
                        }
                    }
                    LastSpectrumData = MinHoldSpectrumData;

                    break;
                default:
                    break;
            }
        }

        private void UpdateDataForAverage()
        {
            if (AverageState.GetValueOrDefault())
            {
                if (AveragedSpectrumData.Length != LastSpectrumData.Length)
                {
                    AveragedSpectrumData = new double[LastSpectrumData.Length];
                }
                for (int i = 0; i < AveragedSpectrumData.Length; i++)
                {
                    AveragedSpectrumData[i] = (AveragedSpectrumData[i] * AverageCounting + LastSpectrumData[i]) / (AverageCounting + 1);
                }

                LastSpectrumData = AveragedSpectrumData;
                AverageCounting++;
            }
        }

        private void UpdateMarkerData()
        {
            for (int i = 0; i < DisplayMarkers.Count; i++)
            {
                switch (DisplayMarkers[i].State)
                {
                    case eMarkerState.Off:
                        break;
                    case eMarkerState.Normal:
                    case eMarkerState.Delta:
                        int dataIndex = (int)Math.Round(DisplayMarkers[i].XIndexScale * LastSpectrumData.Length);
                        dataIndex = Math.Max(0, Math.Min(dataIndex, LastSpectrumData.Length - 1));
                        DisplayMarkers[i].YValue = LastSpectrumData[dataIndex];
                        break;
                    default:
                        break;
                }

            }
        }

        private void UpdateMarkerYRange()
        {
            for (int i = 0; i < SpectrumAreaViewModel.MARKER_NUM; i++)
            {
                Markers[i].UpdateYValueRange(TopLevel, BottomLevel);

            }
        }

        private void UpdateMarkerXRange()
        {
            for (int i = 0; i < SpectrumAreaViewModel.MARKER_NUM; i++)
            {
                Markers[i].UpdateXValueRange(StartFrequency, StopFrequency);
            }
        }

        public void SetCurrentMarkerXIndex(double xIndexScale)
        {
            Marker marker = Markers[mCurrentMarker];
            if (marker.State == eMarkerState.Off)
                return;

            marker.XValue = StartFrequency + (double)xIndexScale * Span;
        }


        public Visibility CurrentMarkerVisibility
        {
            get
            {
                if(Markers[mCurrentMarker].State == eMarkerState.Off)
                {
                    return Visibility.Hidden;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }

        RelayCommand mMarkerPeakSearch;
        public ICommand MarkerPeakSearch
        {
            get { return mMarkerPeakSearch ?? (mMarkerPeakSearch = new RelayCommand(() => DoMarkerPeakSearch())); }
        }
        public void DoMarkerPeakSearch()
        {
            if (Markers[mCurrentMarker].State == eMarkerState.Off || LastSpectrumData.Length == 0)
                return;

            double maxValue = -999;
            int maxIndex = 0;
            for (int i = 0; i < LastSpectrumData.Length; i++)
            {
                if (LastSpectrumData[i] > maxValue)
                {
                    maxValue = LastSpectrumData[i];
                    maxIndex = i;
                }
            }

            SetCurrentMarkerXIndex((double)maxIndex/ LastSpectrumData.Length);
        }

    }

   
    public class AverageStateToDisplay : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as Boolean?).GetValueOrDefault()? "Avg ON": "Avg OFF";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

    public class TraceModeEnumToIndex : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (eTraceMode)Enum.ToObject(typeof(eTraceMode), value);
        }

    }

    public class DetectorTypeEnumToIndex : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (eDetectorType)Enum.ToObject(typeof(eDetectorType), value);
        }

    }

    public class SpectrumMarkerStateToCusor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            eMarkerState state = (eMarkerState)value;
            if (state == eMarkerState.Off)
            {
                return Cursors.Arrow;
            }
            else
            {
                return Cursors.Hand;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
