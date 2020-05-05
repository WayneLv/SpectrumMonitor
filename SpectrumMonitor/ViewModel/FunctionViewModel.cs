using InstrumentDriver.Core.Interfaces;
using InstrumentDriver.Core.Utility;
using InstrumentDriver.SpectrumMonitor;
using SpectrumMonitor.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using InstrumentDriverCore.Interfaces;

namespace SpectrumMonitor.ViewModel
{
    public class FunctionViewModel :AbstractModel
    {
        private readonly SpctrumMonitorViewModel mMainViewModel;
        private bool mIsMornitoring = false;

        private readonly IInstrument mInstr;
        public FunctionViewModel(SpctrumMonitorViewModel viewmodel)
        {
            mMainViewModel = viewmodel;
            mInstr = viewmodel.Instrument;
        }

        private bool mIsBlink = true;
        public bool IsBlink
        {
            get
            {
                return mIsBlink;
            }
            set
            {
                if (value == mIsBlink) return;

                mIsBlink = value;
                NotifyPropertyChanged(() => IsBlink);
            }
        }


        private Visibility mPauseVisibility = Visibility.Hidden;
        public Visibility PauseVisibility
        {
            get { return mPauseVisibility; }
            set
            {
                if (value == mPauseVisibility) return;
                mPauseVisibility = value;
                NotifyPropertyChanged(() => PauseVisibility);
            }
        }

        private Visibility mRestartVisibility = Visibility.Visible;
        public Visibility RestartVisibility
        {
            get { return mRestartVisibility; }
            set
            {
                if (value == mRestartVisibility) return;
                mRestartVisibility = value;
                NotifyPropertyChanged(() => RestartVisibility);
            }
        }

        RelayCommand mPauseUpdate;
        public ICommand PauseUpdate
        {
            get { return mPauseUpdate ?? (mPauseUpdate = new RelayCommand(() => DoPauseUpdate())); }
        }
        public void DoPauseUpdate()
        {
            mMainViewModel.LatestMessage = "Update Paused";
            PauseVisibility = Visibility.Hidden;
            RestartVisibility = Visibility.Visible;

            mIsMornitoring = false;
            IsBlink = true;


        }

        RelayCommand mRestartUpdate;
        public ICommand RestartUpdate
        {
            get { return mRestartUpdate ?? (mRestartUpdate = new RelayCommand(() => DoRestartUpdate())); }
        }
        public void DoRestartUpdate()
        {
            mMainViewModel.LatestMessage = "Update restarted";
            PauseVisibility = Visibility.Visible;
            RestartVisibility = Visibility.Hidden;

            mIsMornitoring = true;
            IsBlink = false;

            Task.Factory.StartNew(delegate { ContinuousSpecSweep(); });

            Task.Factory.StartNew(delegate { SignalCharactersUpdate(); });
        }

        RelayCommand mShowRegisterWindow;
        public ICommand ShowRegisterWindow
        {
            get { return mShowRegisterWindow ?? (mShowRegisterWindow = new RelayCommand(() => DoShowRegisterWindow())); }
        }
        public void DoShowRegisterWindow()
        {
            RegisterControl regWindow = new RegisterControl(mMainViewModel);
            regWindow.ShowDialog();
        }

        RelayCommand mShowSettingPanelWindow;
        public ICommand ShowSettingPanelWindow
        {
            get { return mShowSettingPanelWindow ?? (mShowSettingPanelWindow = new RelayCommand(() => DoShowSettingPanelWindow())); }
        }
        public void DoShowSettingPanelWindow()
        {
            SettingPanelWindow settingWindow = new SettingPanelWindow(mMainViewModel);
            settingWindow.ShowDialog();
        }

        RelayCommand mShowErrors;
        public ICommand ShowErrors
        {
            get { return mShowErrors ?? (mShowErrors = new RelayCommand(() => DoShowErrors())); }
        }
        public void DoShowErrors()
        {
            ErrorInfo errWindow = new ErrorInfo(mMainViewModel);
            errWindow.ShowDialog();
        }

        private RelayCommand mShowDeviceInfo;
        public ICommand ShowDeviceInfo
        {
            get { return mShowDeviceInfo ?? (mShowDeviceInfo = new RelayCommand(() => DoShowDeviceInfo())); }
        }
        public void DoShowDeviceInfo()
        {
            DeviceInfoWindow deviceinfoWindow = new DeviceInfoWindow(mMainViewModel);
            deviceinfoWindow.ShowDialog();
        }

        private void ContinuousSpecSweep()
        {
            double[] spectrum = new double[1];
            while (mIsMornitoring)
            {
                ((SpectrumMonitorInstrument)mInstr).ReadSpectrum(ref spectrum);

                spectrum = Utility.WaveformInterpolation(spectrum, (int)mMainViewModel.SpectrogramAreaControl.SpectrogramMarkerContainer.ActualWidth);
                mMainViewModel.SpectrumAreaViewModel.UpdateData(spectrum);
                mMainViewModel.SpectrogramAreaViewModel.UpdateData(spectrum);

                UpdateSpecDisplay();

                System.Threading.Thread.Sleep(1);

            }
        }

        private CancellationTokenSource mCts = new CancellationTokenSource();
        private Task mUpdateSpectrumDisplayTask;
        private void UpdateSpecDisplay()
        {
            if (mCts.IsCancellationRequested)
            {
                mCts = new CancellationTokenSource();
            }

            CancellationToken ct = mCts.Token;

            mUpdateSpectrumDisplayTask = Task.Factory.StartNew(() =>
            {
                ViewDispatcher.Dispatcher.Invoke(new Action(mMainViewModel.SpectrumAreaControl.UpdateOnNewData));
            }, ct);


            if (!mMainViewModel.SpectrogramAreaControl.IsUpdatingDisplay)
            {
                //updateSpectrogramDisplayTask = Task.Factory.StartNew(() =>
                //{
                //    ViewDispatcher.Dispatcher.Invoke(new Action(mMainViewModel.SpectrogramAreaControl.UpdateOnNewData));
                //}, ct);
                ViewDispatcher.Dispatcher.Invoke(new Action(mMainViewModel.SpectrogramAreaControl.UpdateOnNewData));

            }
            else
            {
                Debug.WriteLine("Data Missed!");
            }
        }

        private void SignalCharactersUpdate()
        {
            List<ISignalCharacters> signallist= new List<ISignalCharacters>();
            while (mIsMornitoring)
            {
                ((SpectrumMonitorInstrument)mInstr).ReadSignalCharacter(ref signallist);

                mMainViewModel.SignalTableAreaViewModel.UpdateData(signallist);

                var cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                Task.Factory.StartNew(() =>
                {
                    ViewDispatcher.Dispatcher.Invoke(new Action(mMainViewModel.SignalTableAreaViewModel.UpdateDisplay));
                }, ct);

                System.Threading.Thread.Sleep(1000);
            }
        }


    }
}
