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
    /// Interaction logic for SpectrumAverageSettingWindow.xaml
    /// </summary>
    public partial class SpectrumAverageSettingWindow : Window
    {
        public SpectrumAverageSettingWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void TextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var textbox = (sender as TextBox);
            double oldValue = Convert.ToSingle(textbox.Text);
            double newValue = oldValue + (e.Delta > 0 ? 1 : -1);
            textbox.Text = newValue.ToString();
        }
    }
}
