using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceFunctionDriver
{
    public class DeviceFactory
    {
        public IDeviceFunction CreateNewDevice(bool simulate = false,string devicetype = "" ,string address = "")
        {
            if (simulate)
            {
                return new SimulateDevice();
            }

            throw new Exception("Unsupported device type");
        }
    }
}
