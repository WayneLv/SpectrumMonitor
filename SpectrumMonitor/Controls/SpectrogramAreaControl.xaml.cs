using Microsoft.Win32.SafeHandles;
using SpectrumMonitor.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
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
using System.Windows.Threading;

namespace SpectrumMonitor.Controls
{
    /// <summary>
    /// Interaction logic for SpectrogramAreaControl.xaml
    /// </summary>
    public partial class SpectrogramAreaControl : UserControl
    {
        private WriteableBitmap mWriteableBmp;
        private SpctrumMonitorViewModel mMainViewModel;
        private SpectrogramAreaViewModel mViewModel;
        private SpectrumAreaViewModel mSpectrumViewModel;

        public bool IsUpdatingDisplay = false;

        public object DisplayLock = new object();
        public SpectrogramAreaControl(SpctrumMonitorViewModel mainViewModel)
        {
            InitializeComponent();

            mainViewModel.SpectrogramAreaControl = this;
            mMainViewModel = mainViewModel;
            mViewModel = mainViewModel.SpectrogramAreaViewModel;
            mSpectrumViewModel = mainViewModel.SpectrumAreaViewModel;

            DataContext = mViewModel;
        }

        private RenderTargetBitmap PreviousBitmap { get; set; }

        public void InitDisplay()
        {

        }

        public void UpdateOnNewData()
        {
            if (mViewModel.SpectrogramData.Count == 0)
                return;

            if (IsUpdatingDisplay)
                return;

            IsUpdatingDisplay = true;

            UpdateSpectrogramDisplay();

            IsUpdatingDisplay = false;
        }


        private void UpdateSpectrogramDisplay()
        {
            //UpdateSpectrogramDisplay_DrawOneByOne();
            UpdateSpectrogramDisplay_PixelsMove();
        }


        private void UpdateSpectrogramDisplay_DrawOneByOne()
        {

            //lock (DisplayLock)
            {
                int width = (int)this.SpectrogramViewPortContainer.ActualWidth;
                int height = (int)this.SpectrogramViewPortContainer.ActualHeight;

                if (mWriteableBmp == null)
                {
                    mWriteableBmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
                }

                mWriteableBmp.Lock();

                int totalTraceCount = mViewModel.SpectrogramData.Count;

                for (int j = 0; j < totalTraceCount; j++)
                {

                    if (height - j < 0)
                    {
                        break;
                    }

                    double[] data = mViewModel.SpectrogramData[totalTraceCount - 1 - j].Value;

                    double xscale = (double)width / (double)data.Length;

                    int lastx = 0;
                    double lastvalue = -999.0;

                    for (int i = 0; i < data.Length; i++)
                    {
                        int x = (int)Math.Round(i * xscale);

                        if (x == lastx && data[i] < lastvalue)
                        {
                            continue;
                        }
                        mWriteableBmp.DrawLine(x, height - j, x + 1, height - j, GetColor(data[i]));
                        lastx = x;
                        lastvalue = data[i];
                    }
                }

                mWriteableBmp.Unlock();

                this.SpectrogramImageViewport.Source = mWriteableBmp;
            }
        }

        private void UpdateSpectrogramDisplay_PixelsMove()
        {
            if (mMainViewModel.SpectrumAreaViewModel.LastSpectrumData == null)
                return;

            //this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {

                double[] data = mMainViewModel.SpectrumAreaViewModel.LastSpectrumData;

                int dWidth = (int)this.SpectrogramViewPortContainer.ActualWidth;
                int dHeight = (int)this.SpectrogramViewPortContainer.ActualHeight;

                if (mWriteableBmp == null || (int)mWriteableBmp.Width != dWidth || (int)mWriteableBmp.Height != dHeight)
                {
                    mWriteableBmp = new WriteableBitmap(dWidth, dHeight, 96, 96, PixelFormats.Pbgra32, null);
                }

                mWriteableBmp.Lock();

                try
                {
                    mWriteableBmp.WritePixels(new Int32Rect(0, 1, dWidth, dHeight - 1),
                        mWriteableBmp.BackBuffer,
                        mWriteableBmp.BackBufferStride * (mWriteableBmp.PixelHeight - 1),
                        mWriteableBmp.BackBufferStride);
                }
                catch (Exception e)
                {
                    mWriteableBmp.Clear();
                }


                double xscale =(double)dWidth / (double)data.Length;

                int lastx = 0;
                double lastvalue = -999.0;

                for (int i = 0; i < data.Length; i++)
                {
                    int x = (int)Math.Round(i * xscale);

                    if (x == lastx && data[i] < lastvalue)
                    {
                        continue;
                    }
                    mWriteableBmp.DrawLine(x, 0, x + 1, 0, GetColor(data[i]));
                    lastx = x;
                    lastvalue = data[i];
                }

                mWriteableBmp.Unlock();

                this.SpectrogramImageViewport.Source = mWriteableBmp;

            //}));
        }



        private void UpdateSpectrogramDisplay_RTB()
        {
            if (mMainViewModel.SpectrumAreaViewModel.LastSpectrumData == null)
                return;
            this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>{

                double[] data = mMainViewModel.SpectrumAreaViewModel.LastSpectrumData;

                double dContainerWidth = this.SpectrogramViewPortContainer.ActualWidth;
                double dContainerHeight = this.SpectrogramViewPortContainer.ActualHeight;            

                double dPixelWidth = dContainerWidth/ data.Length;

                double dCellHeight = 1;
                double dCellWidth = 1;


                DrawingVisual drawingVisual = new DrawingVisual();
                DrawingContext drawingContext = drawingVisual.RenderOpen();


                if (this.PreviousBitmap != null)
                    drawingContext.DrawImage(this.PreviousBitmap, new Rect(0, dCellHeight, dContainerWidth, dContainerHeight - 1));


                for (int i = 0; i < data.Length; i++)

                {

                    double dCellX = Math.Round(i * dPixelWidth);

                    double dY = data[i];

                    Color oColor = this.GetColor(dY);

                    //drawingContext.DrawRectangle(new SolidColorBrush(oColor),
                    //    new Pen(), new Rect(dCellX, 0, dCellWidth, dCellHeight));

                    LinearGradientBrush lineBrush = new LinearGradientBrush();
                    lineBrush.GradientStops.Add(new GradientStop(Colors.Transparent, 0));
                    lineBrush.GradientStops.Add(new GradientStop(oColor, 0.25));
                    lineBrush.GradientStops.Add(new GradientStop(oColor, 0.5));
                    lineBrush.GradientStops.Add(new GradientStop(oColor, 0.75));
                    lineBrush.GradientStops.Add(new GradientStop(Colors.Transparent, 1));

                    drawingContext.DrawRectangle(lineBrush, new Pen(), new Rect(dCellX - 1, 0, dCellWidth + 2, dCellHeight));

                }


           
                drawingContext.Close();

                RenderTargetBitmap rtbCurrent = new RenderTargetBitmap((int)(dContainerWidth), (int)dContainerHeight,
                    96,
                    96,
                    PixelFormats.Pbgra32);

                rtbCurrent.Render(drawingVisual);

                this.SpectrogramImageViewport.Source = rtbCurrent;

                this.PreviousBitmap = rtbCurrent; 
            }));
        }

        private Color GetColor(double value)
        {
            Color topColor, bottomColor;
            double topValue, bottomValue;
            double middleValue = (mViewModel.TopLevel + mViewModel.BottomLevel) / 2;

            if (value > middleValue)
            {
                topColor = Colors.Red;
                bottomColor = Colors.Yellow;
                topValue = mViewModel.TopLevel;
                bottomValue = middleValue;
            }
            else
            {
                topColor = Colors.Yellow;
                bottomColor = Colors.Blue;
                topValue = middleValue ;
                bottomValue = mViewModel.BottomLevel;
            }

            double colorScale = (value - mViewModel.BottomLevel) / (mViewModel.TopLevel - mViewModel.BottomLevel);

            Color valColor = bottomColor + (topColor - bottomColor) * (float)colorScale;
            return valColor;
          }

        private void DisplayLoaded(object sender, RoutedEventArgs e)
        {

            InitMarkers();
        }

        private void InitMarkers()
        {
            for (int i = 0; i < SpectrogramAreaViewModel.MARKER_NUM; i++)
            {
                //Update Marker Range
                mViewModel.Markers[i].UpdateXPosRange(0, (int)SpectrogramViewPortContainer.ActualWidth);
                mViewModel.Markers[i].UpdateZPosRange(0, (int)SpectrogramViewPortContainer.ActualHeight);

                mViewModel.Markers[i].UpdateXValueRange(mSpectrumViewModel.StartFrequency, mSpectrumViewModel.StopFrequency);
                mViewModel.Markers[i].UpdateYValueRange(mSpectrumViewModel.TopLevel, mSpectrumViewModel.BottomLevel);

                mViewModel.Markers[i].XValue = (mSpectrumViewModel.StartFrequency + mSpectrumViewModel.StopFrequency) / 3 * (i+1);
                mViewModel.Markers[i].UpdateXPos();

            }
        }

        public void UpdateMarkersPosRange()
        {
            for (int i = 0; i < SpectrogramAreaViewModel.MARKER_NUM; i++)
            {
                //Update Marker Range
                mViewModel.Markers[i].UpdateXPosRange(0, (int)SpectrogramViewPortContainer.ActualWidth);
                mViewModel.Markers[i].UpdateZPosRange(0, (int)SpectrogramViewPortContainer.ActualHeight);
                mViewModel.Markers[i].UpdateXPos();
            }
        }

        private void SpectrogramMouseClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                //Double click to set Marker
                Point point = Mouse.GetPosition(e.Source as FrameworkElement);

                //Set Z Index to get the trace index first
                mViewModel.SetCurrentMarkerZPos((int)point.Y);

                double xIndexScale = point.X / mWriteableBmp.Width;
                mViewModel.SetCurrentMarkerXIndex(xIndexScale);

            }
        }

        private void SpectrogramAreaControl_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateMarkersPosRange();
        }
    }   
}
