using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstrumentDriver.SpectrumMonitor;

namespace SpectrumMonitor.ViewModel
{
    public class DpxDisplayViewModel : AbstractModel
    {
        SpectrumMonitorInstrument mInstr;
        SpctrumMonitorViewModel mMainViewModel;
        private int mTopColorTimes = 20;
        private int mBottomColorTimes =1;

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

        public void UpdateDisplay()
        {
            
        }

    }
}
