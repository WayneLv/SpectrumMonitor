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
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        private void PswSubmit_Click(object sender, RoutedEventArgs e)
        {
            IntPtr p = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(InputPsw.SecurePassword);
            var PassWord = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(p);
            if (string.IsNullOrEmpty(PassWord) || PassWord != "123456")
            {
                MessageBox.Show("Password is wrong!", "Warning", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return;
            }

            this.DialogResult = true;
        }

        private void PswCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
