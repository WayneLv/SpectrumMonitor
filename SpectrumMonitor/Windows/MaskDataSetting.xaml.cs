using System;
using System.Collections.Generic;
using System.Drawing;
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
using System.Windows.Shapes;
using SpectrumMonitor.ViewModel;

namespace SpectrumMonitor.Windows
{
    /// <summary>
    /// Interaction logic for MaskDataSetting.xaml
    /// </summary>
    public partial class MaskDataSetting : Window
    {
        private WriteableBitmap mWriteableBmpForSpectrumChart;
        private MaskDataListViewModel mViewModel;

        public MaskDataSetting(AbstractModel dataModel)
        {
            InitializeComponent();
            mViewModel = dataModel as MaskDataListViewModel;
            DataContext = dataModel;
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

        private void DisplayLoaded(object sender, RoutedEventArgs e)
        {

        }

        private void SpectrumMouseClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void RemoveSelected_OnClick(object sender, RoutedEventArgs e)
        {
        }

        private void MaskDataSetting_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateDisplay();
        }



        private int[] getWaveformPointArr(double[] data)
        {
            int length = data.Length;
            List<int> point = new List<int>();

            double displayWidth = mWriteableBmpForSpectrumChart.Width;
            double displayHeight = mWriteableBmpForSpectrumChart.Height;
            double levelRange = mViewModel.TopLevel - mViewModel.BottomLevel;

            double scale = displayWidth / length;

            for (int i = 0; i < length; i++)
            {
                point.Add((int)Math.Round(i * scale));
                point.Add((int)(displayHeight - displayHeight * (data[i] - mViewModel.BottomLevel) / levelRange));
            }


            return point.ToArray();
        }

        private int[] getMaskDataPointArr()
        {
            List<int> point = new List<int>();

            double displayWidth = mWriteableBmpForSpectrumChart.Width;
            double displayHeight = mWriteableBmpForSpectrumChart.Height;
            double levelRange = mViewModel.TopLevel - mViewModel.BottomLevel;
            double xRagne = mViewModel.XStop - mViewModel.XStart;

            foreach (var maskdata in mViewModel.MaskDataList)
            {
                point.Add((int)(Math.Round((maskdata.XValue - mViewModel.XStart) / xRagne * displayWidth)));
                point.Add((int)(displayHeight - displayHeight * (maskdata.YValue - mViewModel.BottomLevel) / levelRange));
            }

            return point.ToArray();
        }


        public void DrawRefWaveform()
        {
            if (mViewModel.RefWaveform != null)
            {
                try
                {
                    mWriteableBmpForSpectrumChart.DrawPolylineAa(getWaveformPointArr(mViewModel.RefWaveform), Colors.LightGray);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public void DrawMaskData()
        {
            if (mViewModel.MaskDataList != null && mViewModel.MaskDataList.Count > 0)
            {
                var maskDataArr = getMaskDataPointArr();
                mWriteableBmpForSpectrumChart.DrawPolylineAa(maskDataArr, Colors.GreenYellow);

                int r = 6;
                for (int i = 0; i < maskDataArr.Length/2; i++)
                {
                    int X = maskDataArr[2 * i];
                    int Y = maskDataArr[2 * i+1];
                    mWriteableBmpForSpectrumChart.DrawEllipseCentered(X,Y,r,r,Colors.White);
                }


            }

        }


        public void UpdateDisplay()
        {
            mWriteableBmpForSpectrumChart = BitmapFactory.New((int)WaveformViewPortContainer.ActualWidth, (int)WaveformViewPortContainer.ActualHeight);
            WaveformViewport.Source = mWriteableBmpForSpectrumChart;

            using (mWriteableBmpForSpectrumChart.GetBitmapContext())
            {
                mWriteableBmpForSpectrumChart.Clear();
                DrawScale();

                DrawRefWaveform();

                //DrawMaskData();
            }
        }
    }
}
