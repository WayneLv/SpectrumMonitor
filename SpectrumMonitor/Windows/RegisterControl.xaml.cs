﻿using SpectrumMonitor.ViewModel;
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
    /// Interaction logic for RegisterControl.xaml
    /// </summary>
    public partial class RegisterControl : Window
    {
        private SpctrumMonitorViewModel mMainViewModel;
        RegisterControlViewModel mViewModel;

        public RegisterControl(AbstractModel mainViewModel)
        {
            InitializeComponent();

            if (mainViewModel == null)
            {
                SpctrumMonitorViewModel.ViewDispatcher = this;
                SpctrumMonitorViewModel spctrumMonitorViewModel = new SpctrumMonitorViewModel();
                mMainViewModel = spctrumMonitorViewModel;
                mViewModel = spctrumMonitorViewModel.RegisterControlViewModel;
            }
            else
            {
                mMainViewModel = (SpctrumMonitorViewModel) mainViewModel;
                mViewModel = mMainViewModel?.RegisterControlViewModel;
            }

            DataContext = mViewModel;
            
        }

        

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mViewModel.RegisterGroupIndex = 0;
            mViewModel.RegisterIndex = 0;

            mViewModel.UpdateErrorMessage(true);
        }

        private void AddressAccess_Click(object sender, RoutedEventArgs e)
        {
            AddressAccessWindow addressAccessWindow = new AddressAccessWindow(mMainViewModel);
            addressAccessWindow.ShowDialog();
        }

        private void Control_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ErrorInfo errWindow = new ErrorInfo(mMainViewModel);
            errWindow.ShowDialog();
            mViewModel.UpdateErrorMessage();
        }
    }
}
