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
    /// Interaction logic for SpectrumAmpSettingWindow.xaml
    /// </summary>
    public partial class SpectrumAmpSettingWindow : Window
    {
        public SpectrumAmpSettingWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
       
    }
}
