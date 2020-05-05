using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceFunctionDriver
{
    class SimulateDevice : AbstractDeviceFunction
    {

        internal SimulateDevice()
        {
            mSettingParameters.Add("Start Frequency", 900e6);
            mSettingParameters.Add("Stop Frequency", 2400e6);
            mSettingParameters.Add("Points", 1024);
            mSettingParameters.Add("Average", false);
            mSettingParameters.Add("Average Count", 100);



        }

        public override bool Open(string address = "")
        {
            mInitialized = true;

            return true;
        }

        public override bool Close()
        {
            mInitialized = false;
            return true;
        }

        public override string getDeviceInfo()
        {
            throw new NotImplementedException();
        }
     
        public override string GetErrorInfo()
        {
            return "Simulaton -- No Error";
        }


        public override string GetStatusInfo()
        {
            UInt32 version = 0;
            Qworks.Version(ref version);

            UInt32 devsum = 100;
            Qworks.Initialize(ref devsum);
            return String.Format("Version = {0}; Device Sum = {1}", version, devsum);
            //throw new NotImplementedException();
        }


        public override bool Initialize(bool force = false)
        {
            throw new NotImplementedException();
        }

        public override bool LoadFPGAFile(string fileName, int destination = 0)
        {
            throw new NotImplementedException();
        }

        public override int ReadRegister(int address, int subDevice = 0)
        {
            return 0;
        }

        public override bool ReadSpectrum(ref double[] data, int index = 0)
        {

            data = Utility.SimulateSpectrumData(mSettingParameters["Points"]);
            return true;
        }

        public override bool SetParameter<T>(string paraName, T value)
        {
            return base.SetParameter<T>(paraName, value);
        }

        public override bool WriteRegister(int address, int value, int subDevice = 0)
        {
            throw new NotImplementedException();
        }

    }
}
