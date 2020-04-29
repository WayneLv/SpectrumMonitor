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

        public void InitDisplay()
        {
            mWriteableBmpForSpectrumChart = BitmapFactory.New((int)WaveformViewPortContainer.ActualWidth, (int)WaveformViewPortContainer.ActualHeight);
            WaveformViewport.Source = mWriteableBmpForSpectrumChart;
            DrawScale();
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
            InitDisplay();
        }

        private void SpectrumMouseClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void RemoveSelected_OnClick(object sender, RoutedEventArgs e)
        {
        }
    }
}
