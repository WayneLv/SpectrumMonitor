using InstrumentDriver.Core.Interfaces;
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
    /// Interaction logic for IndicatorAreaControl.xaml
    /// </summary>
    public partial class IndicatorAreaControl : UserControl
    {
        IndicatorViewModel mViewModel;
        public IndicatorAreaControl(SpctrumMonitorViewModel mainViewModel)
        {
            InitializeComponent();
            mainViewModel.IndicatorAreaControl = this;

            mViewModel = mainViewModel.IndicatorViewModel;
            DataContext = mainViewModel;
        }
    }
}
