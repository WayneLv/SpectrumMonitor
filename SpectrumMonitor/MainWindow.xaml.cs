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
using SpectrumMonitor.Controls;

using SpectrumMonitor.ViewModel;
using SpectrumMonitor.Windows;

namespace SpectrumMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SpctrumMonitorViewModel mSpctrumMonitorViewModel;
        public MainWindow()
        {

            InitializeComponent();

            // Set view model for main window
            mSpctrumMonitorViewModel = new SpctrumMonitorViewModel();
            mSpctrumMonitorViewModel.MainWindowInstance = this;
            DataContext = mSpctrumMonitorViewModel;
            SpctrumMonitorViewModel.ViewDispatcher = this;

            MenuArea.Content = new MenuAreaControl(mSpctrumMonitorViewModel);
            FunctionArea.Content = new FunctionAreaControl(mSpctrumMonitorViewModel);
            IndicatorArea.Content = new IndicatorAreaControl(mSpctrumMonitorViewModel);
            SignalTableArea.Content = new SignalTableAreaControl(mSpctrumMonitorViewModel);

            SpectrumArea.Content = new SpectrumAreaControl(mSpctrumMonitorViewModel);
            DpxDisplayArea.Content = new DPXDisplayControl(mSpctrumMonitorViewModel);
            
            SpectrogramArea.Content = new SpectrogramAreaControl(mSpctrumMonitorViewModel);

        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            mSpctrumMonitorViewModel.Instrument.Close();
            System.Environment.Exit(0);
        }

        private void Control_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ErrorInfo errWindow = new ErrorInfo(mSpctrumMonitorViewModel);
            errWindow.ShowDialog();
            mSpctrumMonitorViewModel.UpdateErrorMessage();
        }

        public void DisplaySizeChange(bool restore, string controlName = "")
        {
            if (restore)
            {
                SignalTableArea.Visibility = Visibility.Visible;

                Grid.SetRow(SpectrogramArea, 6);
                Grid.SetColumnSpan(SpectrogramArea, 1);
                Grid.SetRowSpan(SpectrogramArea, 1);
                SpectrogramArea.Visibility = Visibility.Visible;


                Grid.SetColumnSpan(SpectrumArea,1);
                Grid.SetRowSpan(SpectrumArea, 1);
                SpectrumArea.Visibility = mSpctrumMonitorViewModel.DpxDisplayEnabled? Visibility.Hidden: Visibility.Visible;

                Grid.SetColumnSpan(DpxDisplayArea, 1);
                Grid.SetRowSpan(DpxDisplayArea, 1);
                DpxDisplayArea.Visibility = mSpctrumMonitorViewModel.DpxDisplayEnabled ? Visibility.Visible : Visibility.Hidden;
            }
            else
            {
                int maxDisplayRows = 3;
                int maxDisplayCols = 4;

                SignalTableArea.Visibility = Visibility.Hidden;

                if (controlName == "SpectrogramArea")
                {
                    SpectrumArea.Visibility = Visibility.Hidden;
                    DpxDisplayArea.Visibility = Visibility.Hidden;
                    SpectrogramArea.Visibility = Visibility.Visible;

                    Grid.SetRow(SpectrogramArea, 4);
                    Grid.SetColumnSpan(SpectrogramArea, maxDisplayCols);
                    Grid.SetRowSpan(SpectrogramArea, maxDisplayRows);

                }
                else if (controlName == "SpectrumArea")
                {
                    SpectrogramArea.Visibility = Visibility.Hidden;
                    DpxDisplayArea.Visibility = Visibility.Hidden;
                    SpectrumArea.Visibility = Visibility.Visible;

                    Grid.SetColumnSpan(SpectrumArea, maxDisplayCols);
                    Grid.SetRowSpan(SpectrumArea, maxDisplayRows);
                }
                else if (controlName == "DpxDisplayArea")
                {
                    SpectrogramArea.Visibility = Visibility.Hidden;
                    SpectrumArea.Visibility = Visibility.Hidden;
                    SpectrumArea.Visibility = Visibility.Visible;

                    Grid.SetColumnSpan(DpxDisplayArea, maxDisplayCols);
                    Grid.SetRowSpan(DpxDisplayArea, maxDisplayRows);
                }
            }

        }

    }
}
