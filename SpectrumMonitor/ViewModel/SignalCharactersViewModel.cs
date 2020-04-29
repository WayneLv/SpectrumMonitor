using System;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace SpectrumMonitor.ViewModel
{
    public class SignalCharactersViewModel: AbstractModel
    {
        private bool mSyncCdc = false;

        public SignalCharactersViewModel(double freq, double bw, double power, bool isTd)
        {
            Frequency = freq;
            BandWidth = bw;
            IsTdSignal = isTd;
            Power = power;
        }

        public double Frequency { get; } = 1e9;

        public double BandWidth { get; } = 10e6;

        public bool IsTdSignal { get; } = false;

        public double Power { get; } = -100;

        public bool SyncCdc
        {
            get => mSyncCdc;
            set
            {
                mSyncCdc = value;
                NotifyPropertyChanged((() => SyncCdc));
            }
        }
    }

    public sealed class SignalListView : ListView
    {
        protected override void PrepareContainerForItemOverride(System.Windows.DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            if (View is GridView)
            {
                ListViewItem signalview = element as ListViewItem;
                SignalCharactersViewModel signaldata = item as SignalCharactersViewModel;
                if (signaldata == null || signalview == null)
                    return;

                signalview.Foreground = (signaldata.Power > 0) ? Brushes.Red : Brushes.Blue;
                signalview.Background = signaldata.IsTdSignal ? Brushes.DarkKhaki : Brushes.DarkGray;

            }


        }
    }

}
