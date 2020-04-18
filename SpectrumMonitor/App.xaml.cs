using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SpectrumMonitor.Windows;

namespace SpectrumMonitor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private void Application_Startup(object sender, StartupEventArgs e)
        {

            ////Test code -- Start
            //RegisterControl regWindow = new RegisterControl(null);
            //regWindow.ShowDialog();
            //this.Shutdown();
            ////Test code -- End

            MainWindow mainwin = new MainWindow();
            Login loginwindow = new Login();

            if (SpectrumMonitor.Properties.Resources.NeedLogIn == "TRUE1")
            {
                loginwindow.ShowDialog();
                if (!loginwindow.DialogResult.GetValueOrDefault())
                {
                    this.Shutdown();
                }
               
            }

            mainwin.ShowDialog();
        }
    }

}
