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
    /// Interaction logic for DeviceInfo.xaml
    /// </summary>
    public partial class DeviceInfoWindow : Window
    {
        private SpctrumMonitorViewModel mMainViewModel;
        public DeviceInfoWindow(SpctrumMonitorViewModel mainViewModel)
        {
            InitializeComponent();

            mMainViewModel = mainViewModel;
            DataContext = mMainViewModel.DeviceInfoViewModel;
        }

        private void DeviceInfo_OnLoaded(object sender, RoutedEventArgs e)
        {
            mMainViewModel.DeviceInfoViewModel.DoRefreshDeviceInfo();
        }
    }
}
