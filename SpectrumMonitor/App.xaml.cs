using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using InstrumentDriver.Core.Utility;
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
            Configuration.Initialize();

            if (Configuration.MainUI == Configuration.MAINUI_REGCONTROL)
            {
                RegisterControl regWindow = new RegisterControl(null);
                regWindow.ShowDialog();
                regWindow.Close();
                this.Shutdown();
            }

            if (Configuration.MainUI == Configuration.MAINUI_ADDRESSACCESS)
            {
                AddressAccessWindow addrWindow = new AddressAccessWindow(null);
                addrWindow.ShowDialog();
                this.Shutdown();
            }

            MainWindow mainwin = new MainWindow();
            Login loginwindow = new Login();

            if (Configuration.NeedLogin)
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
