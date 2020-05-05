using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using InstrumentDriver.Core.Interfaces;
using InstrumentDriver.SpectrumMonitor;

namespace SpectrumMonitor.ViewModel
{
    public class DeviceInfo
    {
        public DeviceInfo(string name, string value)
        {
            Name = name;
            Value = value;
        }
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class DeviceInfoViewModel : AbstractModel
    {
        private readonly SpctrumMonitorViewModel mMainViewModel;
        private readonly IInstrument mInstr;

        private ObservableCollection<DeviceInfo> mDeviceInfoList = new ObservableCollection<DeviceInfo>();

        public DeviceInfoViewModel(SpctrumMonitorViewModel viewmodel)
        {
            mMainViewModel = viewmodel;
            mInstr = viewmodel.Instrument;

        }

        public ObservableCollection<DeviceInfo> DeviceInfoList => mDeviceInfoList;


        RelayCommand mRefreshDeviceInfoCommand;

        public ICommand RefreshDeviceInfo
        {
            get
            {
                return mRefreshDeviceInfoCommand ??
                       (mRefreshDeviceInfoCommand = new RelayCommand(() => DoRefreshDeviceInfo()));
            }
        }

        public void DoRefreshDeviceInfo()
        {
            mDeviceInfoList.Clear();
            var infoDict = (mInstr.RegDriver as QWorksRegDriver)?.GetDeviceInfo();
            if (infoDict == null)
                return;
            foreach (var info in infoDict)
            {
                mDeviceInfoList.Add(new DeviceInfo(info.Key, info.Value));
            }

            NotifyPropertyChanged(()=> DeviceInfoList);
        }
    }
}
