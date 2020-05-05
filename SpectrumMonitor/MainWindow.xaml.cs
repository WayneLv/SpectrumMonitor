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
            DataContext = mSpctrumMonitorViewModel;
            SpctrumMonitorViewModel.ViewDispatcher = this;

            MenuArea.Content = new MenuAreaControl(mSpctrumMonitorViewModel);
            FunctionArea.Content = new FunctionAreaControl(mSpctrumMonitorViewModel);
            IndicatorArea.Content = new IndicatorAreaControl(mSpctrumMonitorViewModel);
            SignalTableArea.Content = new SignalTableAreaControl(mSpctrumMonitorViewModel);
            SpectrumArea.Content = new SpectrumAreaControl(mSpctrumMonitorViewModel);
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
    }
}
