using InstrumentDriver.Core.Interfaces;

namespace SpectrumMonitor.ViewModel
{
    public class MenuViewModel : AbstractModel
    {
        IInstrument mInstr;
        public MenuViewModel(SpctrumMonitorViewModel viewmodel)
        {
            mInstr = viewmodel.Instrument;
        }
    }
}
