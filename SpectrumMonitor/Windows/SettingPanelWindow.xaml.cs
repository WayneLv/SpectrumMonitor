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
    /// Interaction logic for SettingWindow.xaml
    /// </summary>
    public partial class SettingPanelWindow : Window
    {

        public SettingPanelWindow(SpctrumMonitorViewModel mainViewMode)
        {
            InitializeComponent();

            DataContext = mainViewMode.SpectrumAreaViewModel;
        }
    }
}
