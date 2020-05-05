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
    /// Interaction logic for AddressAccessWindow.xaml
    /// </summary>
    public partial class AddressAccessWindow : Window
    {
        private SpctrumMonitorViewModel mMainViewModel;
        RegisterControlViewModel mViewModel;
        public AddressAccessWindow(AbstractModel mainViewModel)
        {
            InitializeComponent();

            if (mainViewModel == null)
            {
                SpctrumMonitorViewModel.ViewDispatcher = this;
                SpctrumMonitorViewModel spctrumMonitorViewModel = new SpctrumMonitorViewModel();
                mMainViewModel = spctrumMonitorViewModel;
                mViewModel = spctrumMonitorViewModel.RegisterControlViewModel;
            }
            else
            {
                mMainViewModel = mainViewModel as SpctrumMonitorViewModel;
                mViewModel = mMainViewModel?.RegisterControlViewModel;
            }

            DataContext = mViewModel;
        }

        private void AddressAccessWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            mViewModel.UpdateErrorMessage(true);
        }

        private void Control_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ErrorInfo errWindow = new ErrorInfo(mMainViewModel);
            errWindow.ShowDialog();
            mViewModel.UpdateErrorMessage();
        }
    }
}
