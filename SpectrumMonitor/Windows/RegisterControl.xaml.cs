using SpectrumMonitor.ViewModel;
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

namespace SpectrumMonitor.Windows
{
    /// <summary>
    /// Interaction logic for RegisterControl.xaml
    /// </summary>
    public partial class RegisterControl : Window
    {
        RegisterControlViewModel mViewModel;

        public RegisterControl(AbstractModel mainViewModel)
        {
            InitializeComponent();

            //Test code start
            //SpctrumMonitorViewModel mSpctrumMonitorViewModel = new SpctrumMonitorViewModel();
            //Test code end

            mViewModel = (RegisterControlViewModel)mainViewModel;
            DataContext = mViewModel;
            
            //SpctrumMonitorViewModel.ViewDispatcher = this;

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mViewModel.RegisterGroupIndex = 0;
            mViewModel.RegisterIndex = 0;

        }
    }
}
