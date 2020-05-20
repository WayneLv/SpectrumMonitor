using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using InstrumentDriver.SpectrumMonitor;
using SpectrumMonitor.Windows;

namespace SpectrumMonitor.ViewModel
{
    public class DpxDisplayViewModel : AbstractModel
    {
        SpectrumMonitorInstrument mInstr;
        SpctrumMonitorViewModel mMainViewModel;
        private int mTopColorTimes = 20;
        private int mBottomColorTimes =1;
        private bool mIsMaxDisplayed = false;

        public DpxDisplayViewModel(SpctrumMonitorViewModel mainviewmodel)
        {
            mMainViewModel = mainviewmodel;
            mInstr = mainviewmodel.Instrument as SpectrumMonitorInstrument;


        }

        public int TopColorTimes
        {
            get => mTopColorTimes;
            set
            {
                if (value <= mBottomColorTimes)
                    value = mBottomColorTimes + 1;

                mTopColorTimes = value;
                NotifyPropertyChanged(()=> TopColorTimes);
            }
        }

        public int BottomColorTimes
        {
            get => mBottomColorTimes;
            set
            {
                if (value >= mTopColorTimes)
                    value = mTopColorTimes - 1;
                if (value < 1)
                    value = 1;

                mBottomColorTimes = value;
                NotifyPropertyChanged(() => BottomColorTimes);
            }
        }

        public double TopLevel
        {
            get { return mMainViewModel.SpectrumAreaViewModel.TopLevel; }
            set { mMainViewModel.SpectrumAreaViewModel.TopLevel = value; }
        }
        

        public double BottomLevel
        {
            get { return mMainViewModel.SpectrumAreaViewModel.BottomLevel; }
            set { mMainViewModel.SpectrumAreaViewModel.BottomLevel = value; }
        }

        public double XStart => mMainViewModel.SpectrumAreaViewModel.StartFrequency;

        public double XStop => mMainViewModel.SpectrumAreaViewModel.StopFrequency;

        public double Center => mMainViewModel.SpectrumAreaViewModel.Center;

        public int[,] DpxData { get; set; }

        public void UpdateData(int[,] dpxdata)
        {
            DpxData = dpxdata;
        }

        public bool IsMaxDisplayed
        {
            get => mIsMaxDisplayed;
            set
            {
                mIsMaxDisplayed = value;
                NotifyPropertyChanged(()=> IsMaxDisplayed);
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

            var mainWindow = MainWindow.GetWindow(mMainViewModel.DPXDisplayControl) as SpectrumMonitor.MainWindow;
            mainWindow.DisplaySizeChange(!IsMaxDisplayed, "DpxDisplayArea");
        }

    }
}
