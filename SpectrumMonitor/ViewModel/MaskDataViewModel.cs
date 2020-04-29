using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SpectrumMonitor.Windows;

namespace SpectrumMonitor.ViewModel
{
    public class MaskData: AbstractModel
    {
        public MaskData(double xvalue,double yvalue)
        {
            XValue = xvalue;
            YValue = yvalue;
        }


        public double XValue { get; set; }
        public double YValue { get; set; }

        public int XPos { get; set; }

        public int YPos { get; set; }
    }

    public class MaskDataListViewModel : AbstractModel
    {
        private ObservableCollection<MaskData> mMaskDataList = new ObservableCollection<MaskData>();
        private string mListName= string.Empty;
        private double[] mRefWaveform;

        public MaskDataListViewModel(string listName)
        {
            mListName = listName;
        }

        public string ListName
        {
            get => mListName;
            set => mListName = value;
        }

        public ObservableCollection<MaskData> MaskDataList
        {
            get => mMaskDataList;
            set => mMaskDataList = value;
        }


        public string XLable { get; set; } = "Frequency";
        public string YLable { get; set; } = "Power";


        RelayCommand mAddOnePoint;
        public ICommand AddOnePoint
        {
            get { return mAddOnePoint ?? (mAddOnePoint = new RelayCommand(() => DoAddOnePoint())); }
        }
        public void DoAddOnePoint()
        {

            double lastX = 1e9;
            double lastY = 0;
            if (MaskDataList.Count != 0)
            {
                 lastX = MaskDataList.Last().XValue;
                 lastY = MaskDataList.Last().YValue;
            }

            MaskDataList.Add(new MaskData(lastX+1e6,lastY));
            NotifyPropertyChanged((() => MaskDataList));
        }

        RelayCommand mRemoveSelected;
        public ICommand RemoveSelected
        {
            get { return mRemoveSelected ?? (mRemoveSelected = new RelayCommand(() => DoRemoveSelected())); }
        }
        public void DoRemoveSelected()
        {

        }

        RelayCommand mRemoveAll;
        public ICommand RemoveAll
        {
            get { return mRemoveAll ?? (mRemoveAll = new RelayCommand(() => DoRemoveAll())); }
        }
        public void DoRemoveAll()
        {
            MaskDataList.Clear();
            NotifyPropertyChanged((() => MaskDataList));
        }


        public void SetRefWaveform(double[] waveform)
        {
            mRefWaveform = waveform;
        }

        public double[] GetMaskArray(double start, double stop, int points)
        {
            return new double[points];
        }



        
    }
}
