using InstrumentDriver.Core.Interfaces;

namespace SpectrumMonitor.ViewModel
{
    public class IndicatorViewModel: AbstractModel
    {
        IInstrument mInstr;

        public IndicatorViewModel(SpctrumMonitorViewModel mainViewModel)
        {
            mInstr = mainViewModel.Instrument;
        }


    }
}
