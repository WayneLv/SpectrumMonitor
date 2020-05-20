using InstrumentDriver.Core.Utility;
using SpectrumMonitor.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SpectrumMonitor.Controls
{
    /// <summary>
    /// Interaction logic for SpectrumAreaControl.xaml
    /// </summary>
    /// 
    public partial class SpectrumAreaControl : UserControl
    {
        private WriteableBitmap mWriteableBmpForSpectrumChart;
        private SpectrumAreaViewModel mViewModel;

        private SpectrumMarkerIcon[] mMarkerIcon = new SpectrumMarkerIcon[SpectrumAreaViewModel.MARKER_NUM];
        private SpectrumMarkerIcon[] mDeltaMarkerIcon = new SpectrumMarkerIcon[SpectrumAreaViewModel.MARKER_NUM];

        public SpectrumAreaControl(SpctrumMonitorViewModel mainViewMode)
        {
            InitializeComponent();

            mainViewMode.SpectrumAreaControl = this;

            mViewModel = mainViewMode.SpectrumAreaViewModel;
            DataContext = mViewModel;

            TopLevelLabel.ViewModel = mViewModel;
            BottomLevelLabel.ViewModel = mViewModel;
            AverageStateLabel.ViewModel = mViewModel;
            StartFreqLabel.ViewModel = mViewModel;
            StopFreqLabel.ViewModel = mViewModel;
            RBWLabel.ViewModel = mViewModel;

        }

        public void InitDisplay()
        {
            mWriteableBmpForSpectrumChart = BitmapFactory.New((int)SpectrumViewPortContainer.ActualWidth, (int)SpectrumViewPortContainer.ActualHeight);
            SpectrumImageViewport.Source = mWriteableBmpForSpectrumChart;
            DrawScale();
        }

        public void UpdateOnNewData()
        {
            if (mWriteableBmpForSpectrumChart != null)
            {
                if ((int)mWriteableBmpForSpectrumChart.Width != (int)SpectrumViewPortContainer.ActualWidth 
                    || (int)mWriteableBmpForSpectrumChart.Height != (int)SpectrumViewPortContainer.ActualHeight)
                {
                    mWriteableBmpForSpectrumChart = BitmapFactory.New((int) SpectrumViewPortContainer.ActualWidth,
                        (int) SpectrumViewPortContainer.ActualHeight);
                    SpectrumImageViewport.Source = mWriteableBmpForSpectrumChart;
                }

                using (mWriteableBmpForSpectrumChart.GetBitmapContext())
                {
                    mWriteableBmpForSpectrumChart.Clear();
                    DrawScale();
                    mWriteableBmpForSpectrumChart.DrawPolylineAa(getPointArr(mViewModel.LastSpectrumData), Colors.YellowGreen);
                }
            }
        }

        private void DrawScale()
        {
            var width = mWriteableBmpForSpectrumChart.Width;
            var height = mWriteableBmpForSpectrumChart.Height;
            for (int i = 0; i < 9; i++)
            {

                int x1 = 0;
                int y1 = (int)((i + 1) * (height / 10));
                int x2 = (int)width;
                int y2 = y1;
                mWriteableBmpForSpectrumChart.DrawLine(x1, y1, x2, y2, Colors.Gray);

                x1 = (int)((i + 1) * (width / 10));
                y1 = 0;
                x2 = x1;
                y2 = (int)height;
                mWriteableBmpForSpectrumChart.DrawLine(x1, y1, x2, y2, Colors.Gray);

            }

        }
        
        private int[] getPointArr(double[] data)
        {
            int length = data.Length;
            List<int> point = new List<int>();

            double displayWidth = mWriteableBmpForSpectrumChart.Width;
            double displayHeight = mWriteableBmpForSpectrumChart.Height;
            double levelRange = mViewModel.TopLevel - mViewModel.BottomLevel;

            double scale = displayWidth / length;

            for (int i = 0; i < length; i++)
            {
                point.Add((int)Math.Round(i* scale));
                point.Add((int)(displayHeight - displayHeight * (data[i]-mViewModel.BottomLevel)/levelRange) );
            }


            return point.ToArray();
        }

        private void DisplayLoaded(object sender, RoutedEventArgs e)
        {
            InitDisplay();

            InitMarkers();
        }

        private void InitMarkers()
        {
            for (int i = 0; i < SpectrumAreaViewModel.MARKER_NUM; i++)
            {
                //Update Marker Range
                mViewModel.Markers[i].UpdateXPosRange(0, (int)SpectrumViewPortContainer.ActualWidth);
                mViewModel.Markers[i].UpdateYPosRange(0, (int)SpectrumViewPortContainer.ActualHeight);

                mViewModel.Markers[i].UpdateXValueRange(mViewModel.StartFrequency, mViewModel.StopFrequency);
                mViewModel.Markers[i].UpdateYValueRange(mViewModel.TopLevel, mViewModel.BottomLevel);

                mViewModel.Markers[i].XValue = (mViewModel.StartFrequency + mViewModel.StopFrequency) / 3;
                mViewModel.Markers[i].UpdateXPos();
               
            }
        }

        public void UpdateMarkersPosRange()
        {
            for (int i = 0; i < SpectrumAreaViewModel.MARKER_NUM; i++)
            {
                //Update Marker Range
                mViewModel.Markers[i].UpdateXPosRange(0, (int)SpectrumViewPortContainer.ActualWidth);
                mViewModel.Markers[i].UpdateYPosRange(0, (int)SpectrumViewPortContainer.ActualHeight);
                mViewModel.Markers[i].UpdateXPos();
                mViewModel.Markers[i].UpdateYPos();
            }
        }

        private void MarkerFrequency_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            NumericBox numericBox = sender as NumericBox;
            double currentValue = Utility.FrequencyStringToValue(numericBox.Text);
            double stepvalue = mViewModel.Span / 100;
            double newValue = currentValue + (e.Delta > 0 ? stepvalue : -stepvalue);
            numericBox.Text = Utility.FrequencyValueToString(newValue);

        }

        private void SpectrumMouseClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                //Double click to set Marker
                Point point = Mouse.GetPosition(e.Source as FrameworkElement);
                double xIndexScale = point.X / mWriteableBmpForSpectrumChart.Width;
                mViewModel.SetCurrentMarkerXIndex(xIndexScale);
            }
        }

        private void SpectrumAreaControl_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateOnNewData();
            UpdateMarkersPosRange();
        }
    }
}
