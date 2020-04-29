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
    /// Interaction logic for SignalTableAreaControl.xaml
    /// </summary>
    public partial class SignalTableAreaControl : UserControl
    {
        public SignalTableAreaControl(SpctrumMonitorViewModel mainViewModel)
        {
            InitializeComponent();
            mainViewModel.SignalTableAreaControl = this;

            DataContext = mainViewModel.SignalTableAreaViewModel;
        }

    }
}
