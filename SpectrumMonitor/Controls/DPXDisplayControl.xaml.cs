using System;
using System.Collections.Generic;
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
using SpectrumMonitor.ViewModel;

namespace SpectrumMonitor.Controls
{
    /// <summary>
    /// Interaction logic for DPXDisplayControl.xaml
    /// </summary>
    public partial class DPXDisplayControl : UserControl
    {
        private WriteableBitmap mWriteableBmp;
        private SpctrumMonitorViewModel mMainViewModel;
        private DpxDisplayViewModel mViewModel;

        public bool IsUpdatingDisplay { get; set; } = false;
        public DPXDisplayControl(SpctrumMonitorViewModel mainViewMode)
        {
            InitializeComponent();

            mMainViewModel = mainViewMode;
            mainViewMode.DPXDisplayControl = this;

            DataContext = mainViewMode.DpxDisplayViewModel;

            mViewModel = mainViewMode.DpxDisplayViewModel;
        }

        public void UpdateOnNewData()
        {
            if (mViewModel.DpxData.Length == 0)
                return;

            if (IsUpdatingDisplay)
                return;

            IsUpdatingDisplay = true;

            UpdateDpxDisplay();

            IsUpdatingDisplay = false;
        }

        private void UpdateDpxDisplay()
        {
            DrawDpx();
        }

        private void DrawDpx()
        {

            //lock (DisplayLock)
            {
                int width = (int)this.DpxViewPortContainer.ActualWidth;
                int height = (int)this.DpxViewPortContainer.ActualHeight;

                int datawidth = mViewModel.DpxData.GetLength(0);
                int dataheight = mViewModel.DpxData.GetLength(1);

                //int datawidth = (int)this.DpxViewPortContainer.ActualWidth;
                //int dataheight = (int)this.DpxViewPortContainer.ActualHeight;

                if (mWriteableBmp == null)
                {
                    mWriteableBmp = new WriteableBitmap(datawidth/*width*/, dataheight/*height*/, 96, 96, PixelFormats.Pbgra32, null);
                }

                mWriteableBmp.Lock();

                int totalFreqCount = mViewModel.DpxData.GetLength(0); //Get freq numbers
                int totalPowerCount = mViewModel.DpxData.GetLength(1); //Get power numbers

                for (int i = 0; i < totalFreqCount; i++)
                {
                    for (int j = 0; j < totalPowerCount; j++)
                    {
                        mWriteableBmp.DrawLine(i, dataheight -j, i, dataheight-j-1, GetColor(mViewModel.DpxData[i,j]));
                    }
                }

                mWriteableBmp.Unlock();

                var scaledBitmap = mWriteableBmp.Resize(width, height, WriteableBitmapExtensions.Interpolation.Bilinear);
                DrawScale(scaledBitmap);

                this.DpxImageViewport.Source = scaledBitmap;
            }
        }


        private void DrawScale(WriteableBitmap wb)
        {
            var width = wb.Width;
            var height = wb.Height;
            for (int i = 0; i < 9; i++)
            {

                int x1 = 0;
                int y1 = (int)((i + 1) * (height / 10));
                int x2 = (int)width;
                int y2 = y1;
                wb.DrawLine(x1, y1, x2, y2, Colors.Gray);

                x1 = (int)((i + 1) * (width / 10));
                y1 = 0;
                x2 = x1;
                y2 = (int)height;
                wb.DrawLine(x1, y1, x2, y2, Colors.Gray);
            }

        }

        private Color GetColor(int value)
        {
            if (value == 0)
                return Colors.Black;

            Color topColor, bottomColor;
            int topValue, bottomValue;
            int middleValue = (mViewModel.TopColorTimes + mViewModel.BottomColorTimes) / 2;

            if (value > middleValue)
            {
                topColor = Colors.Red;
                bottomColor = Colors.Yellow;
                topValue = mViewModel.TopColorTimes;
                bottomValue = middleValue;
            }
            else
            {
                topColor = Colors.Yellow;
                bottomColor = Colors.Blue;
                topValue = middleValue;
                bottomValue = mViewModel.BottomColorTimes;
            }

            double colorScale = (double)(value - mViewModel.BottomColorTimes) / (double)(mViewModel.TopColorTimes - mViewModel.BottomColorTimes);

            Color valColor = bottomColor + (topColor - bottomColor) * (float)colorScale;
            return valColor;
        }

        private void DpxMouseClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void DisplaySizeChanged(object sender, RoutedEventArgs e)
        {

        }
    }
}
