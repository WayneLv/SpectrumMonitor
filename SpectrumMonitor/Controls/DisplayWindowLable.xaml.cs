using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for DisplayWindowLable.xaml
    /// </summary>
    public partial class DisplayWindowLable : UserControl
    {
        public Type UserWindowType { get; set; }
        public INotifyPropertyChanged ViewModel { get; set; }

        public DisplayWindowLable()
        {
            InitializeComponent();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            var source = e.OriginalSource;
            Window win = Activator.CreateInstance(this.UserWindowType) as Window;
            if (win != null)
            {
                win.DataContext = ViewModel;
                win.ShowDialog();
            }
        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            Background = new SolidColorBrush(Colors.GreenYellow);
            Foreground = new SolidColorBrush(Colors.Black);
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            Background = new SolidColorBrush(Colors.Black);
            Foreground = new SolidColorBrush(Colors.GreenYellow);
        }
    }
}
