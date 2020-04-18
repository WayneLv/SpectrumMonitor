using InstrumentDriver.Core.Interfaces;

namespace SpectrumMonitor.ViewModel
{
    public class SettingPanelViewModel : AbstractModel
    {
        IInstrument mInstr;
        SpctrumMonitorViewModel mMainViewModel;
        public SettingPanelViewModel(SpctrumMonitorViewModel mainViewModel)
        {
            mInstr = mainViewModel.Instrument;
            mMainViewModel = mainViewModel;
        }

        

    }
}
