using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceFunctionDriver
{
    public interface IDeviceFunction
    {
        bool Initialized { get; }

        bool Open(string address = "");
        bool Initialize(bool force = false);
        bool Close();

        string getDeviceInfo();
        string GetErrorInfo();
        string GetStatusInfo();

        bool SetParameter<T>(string paraName, T value);
        T GetParameter<T>(string paraName);
        List<string> GetParameterList();

        bool WriteRegister(int address, int value, int subDevice = 0);
        int ReadRegister(int address, int subDevice = 0);

        bool LoadFPGAFile(string fileName, int destination = 0);

        bool ReadSpectrum(ref double[] data, int index = 0);

    }  
}
