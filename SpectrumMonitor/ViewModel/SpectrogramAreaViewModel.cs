using InstrumentDriver.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace SpectrumMonitor.ViewModel
{
    public class SpectrogramAreaViewModel : AbstractModel
    {
        public static readonly int MARKER_NUM = 2;

        IInstrument mInstr;
        SpctrumMonitorViewModel mMainViewModel;
        SpectrumAreaViewModel mSpectrumViewModel;

        private int mCurrentMarker = 0;
        private readonly List<string> mMarkerItems = new List<string>();
        private readonly List<string> mMarkerStateItems = new List<string> { "Off", "On"/*"Normal", "Delta" */};
        public Marker[] Markers { get; } = new Marker[MARKER_NUM];
        public ObservableCollection<Marker> mDisplayMarkers = new ObservableCollection<Marker>();

        public DateTime StartTime = new DateTime();
        private const double MAX_TIME_IN_SECOND = 30.0; // maximum 30second buffer
        public List<KeyValuePair<DateTime, double[]>> SpectrogramData = new List<KeyValuePair<DateTime, double[]>>();

        public SpectrogramAreaViewModel(SpctrumMonitorViewModel viewmodel)
        {
            mInstr = viewmodel.Instrument;
            mMainViewModel = viewmodel;
            mSpectrumViewModel = viewmodel.SpectrumAreaViewModel;

            for (int i = 0; i < MARKER_NUM; i++)
            {
                Markers[i] = new Marker(mMainViewModel);
                Markers[i].Index = i;
                Markers[i].Type = eMarkerType.Spetrogram;

                mMarkerItems.Add("Marker" + (i + 1).ToString());
            }
        }
        public double TopLevel
        {
            get { return mMainViewModel.SpectrumAreaViewModel.TopLevel; }
        }

        public double BottomLevel
        {
            get { return mMainViewModel.SpectrumAreaViewModel.BottomLevel; }
        }

        public void NotifyColorBarChange()
        {
            NotifyPropertyChanged(() => TopLevel);
            NotifyPropertyChanged(() => BottomLevel);
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


        public List<string> MarkerStateItems
        {
            get { return mMarkerStateItems; }
        }

        public void ClearSpectrogramData()
        {
            SpectrogramData.Clear();
        }

        public void UpdateData(double[] newspectrum)
        {
            if (newspectrum == null)
                return;

            UpdateSpectrogramData(newspectrum);

            UpdateMarkerData();
        }


        private void UpdateSpectrogramData(double[] newspectrum)
        {
            if (SpectrogramData.Count == 0) // The first spectrum after clear
            {
                StartTime = DateTime.Now;
            }

            DateTime curTime = DateTime.Now;
            SpectrogramData.Add(new KeyValuePair<DateTime, double[]>(curTime, newspectrum));

            if ((curTime - StartTime).TotalSeconds > MAX_TIME_IN_SECOND)
            {
                SpectrogramData.RemoveAt(0);
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
                        int zIndex = SpectrogramData.Count - 1 - DisplayMarkers[i].ZPos;
                        if (zIndex < 0)
                            zIndex = 0;

                        DisplayMarkers[i].Time = SpectrogramData[zIndex].Key;
                        DisplayMarkers[i].ZValue = (double)(DisplayMarkers[i].Time - StartTime).TotalSeconds;

                        double[] data = SpectrogramData[zIndex].Value;
                        int dataIndex = (int)Math.Round(DisplayMarkers[i].XIndexScale * data.Length);
                        dataIndex = Math.Max(0, Math.Min(dataIndex, data.Length - 1));
                        DisplayMarkers[i].YValue = data[dataIndex];
                        break;
                    default:
                        break;
                }

            }
        }

        public void SetCurrentMarkerXIndex(double xIndexScale)
        {
            Marker marker = Markers[mCurrentMarker];
            if (marker.State == eMarkerState.Off)
                return;

            marker.XValue = mSpectrumViewModel.StartFrequency + (double)xIndexScale * mSpectrumViewModel.Span;
        }

        public void SetCurrentMarkerZPos(int zpos)
        {
            Marker marker = Markers[mCurrentMarker];
            if (marker.State == eMarkerState.Off)
                return;

            marker.ZPos = zpos;
        }


        public Visibility CurrentMarkerVisibility
        {
            get
            {
                if (Markers[mCurrentMarker].State == eMarkerState.Off)
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
            if (Markers[mCurrentMarker].State == eMarkerState.Off || SpectrogramData.Count == 0)
                return;

            double maxValue = -999;
            int maxZIndex = 0;
            int maxXIndex = 0;
            for (int i = 0; i < SpectrogramData.Count; i++)
            {
                for (int j = 0; j < SpectrogramData[i].Value.Length; j++)
                {
                    if (SpectrogramData[i].Value[j] > maxValue)
                    {
                        maxValue = SpectrogramData[i].Value[j];
                        maxZIndex = i;
                        maxXIndex = j;
                    }
                }
            }

            SetCurrentMarkerXIndex((double)maxXIndex / SpectrogramData[0].Value.Length);
            SetCurrentMarkerZPos(maxZIndex);
        }

        private bool mIsMaxDisplayed = false;
        public bool IsMaxDisplayed
        {
            get => mIsMaxDisplayed;
            set
            {
                mIsMaxDisplayed = value;
                NotifyPropertyChanged(() => IsMaxDisplayed);
            }
        }

        private RelayCommand mDisplaySizeChange;
        public ICommand DisplaySizeChange
        {
            get { return mDisplaySizeChange ?? (mDisplaySizeChange = new RelayCommand(() => DoDisplaySizeChange())); }
        }
        public void DoDisplaySizeChange()
        {
            IsMaxDisplayed = !IsMaxDisplayed;

            var mainWindow = MainWindow.GetWindow(mMainViewModel.SpectrogramAreaControl) as SpectrumMonitor.MainWindow;
            mainWindow.DisplaySizeChange(!IsMaxDisplayed, "SpectrogramArea");
        }

    }

    public class SpectrogramMarkerStateToCusor : IValueConverter
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
                return Cursors.Cross;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

}
