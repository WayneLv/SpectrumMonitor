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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SpectrumMonitor.Controls
{
    /// <summary>
    /// Interaction logic for MenuAreaControl.xaml
    /// </summary>
    public partial class MenuAreaControl : UserControl
    {
        public MenuAreaControl(SpctrumMonitorViewModel mainViewModel)
        {
            InitializeComponent();
            mainViewModel.MenuAreaControl = this;

            DataContext = mainViewModel.MenuViewModel;
        }
    }
}
