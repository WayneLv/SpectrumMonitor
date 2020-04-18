using InstrumentDriver.Core.Interfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using InstrumentDriverCore.Interfaces;

namespace SpectrumMonitor.ViewModel
{
    public class SignalTableAreaViewModel : AbstractModel
    {
        IInstrument mInstr;
        private List<ISignalCharacters> mSignalCharacters;
        private ObservableCollection<SignalCharactersViewModel> mDisplaySignalList = new ObservableCollection<SignalCharactersViewModel>();

        public SignalTableAreaViewModel(SpctrumMonitorViewModel viewmodel)
        {
            mInstr = viewmodel.Instrument;
        }

        public void UpdateData(List<ISignalCharacters> signalCharacters)
        {
            mSignalCharacters = signalCharacters;
        }


        public void UpdateDisplay()
        {
            mDisplaySignalList.Clear();

            foreach (var sc in mSignalCharacters)
            {
                mDisplaySignalList.Add(new SignalCharactersViewModel(sc.Frequency, sc.BandWidth, sc.Power, sc.IsTDSignal));
            }

            NotifyPropertyChanged(() => DisplaySignalList);
        }

        public ObservableCollection<SignalCharactersViewModel> DisplaySignalList
        {
            get { return mDisplaySignalList; }
            set
            {
                mDisplaySignalList = value;
                NotifyPropertyChanged(() => DisplaySignalList);
            }
        }


    }
}
