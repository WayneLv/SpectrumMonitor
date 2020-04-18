using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceFunctionDriver
{
    abstract class AbstractDeviceFunction : IDeviceFunction
    {
        protected bool mInitialized = false;
        protected Dictionary<string, dynamic> mSettingParameters = new Dictionary<string, dynamic>();

        public abstract bool Close();
        public abstract string getDeviceInfo();
        public abstract string GetErrorInfo();       
        public abstract string GetStatusInfo();
        public abstract bool Initialize(bool force = false);
        public abstract bool LoadFPGAFile(string fileName, int destination = 0);
        public abstract bool Open(string address = "");
        public abstract int ReadRegister(int address, int subDevice = 0);
        public abstract bool ReadSpectrum(ref double[] data, int index = 0);
        public abstract bool WriteRegister(int address, int value, int subDevice = 0);


        public bool Initialized
        {
            get
            {
                return mInitialized;
            }
        }

        public virtual T GetParameter<T>(string paraName)
        {
            if (mSettingParameters.ContainsKey(paraName))
            {
                return mSettingParameters[paraName];
            }
            else
            {
                throw new Exception("Unsupported parameter -- " + paraName);
            }
        }

        public virtual bool SetParameter<T>(string paraName, T value)
        {
            if (!mSettingParameters.ContainsKey(paraName))
            {
                throw new Exception("Unsupported parameter -- " + paraName);
            }

            if (typeof(T) != mSettingParameters[paraName].GetType())
            {
                throw new Exception(string.Format("Input value type {0} is not correct, requires a {1}", typeof(T), mSettingParameters[paraName].GetType()));
            }

            mSettingParameters[paraName] = value;

            return true;
        }

        public virtual List<string> GetParameterList()
        {
            List<string> list = new List<string>();
            foreach (var item in mSettingParameters)
            {
                list.Add(String.Format("{0}:{1}", item.Key, item.Value));
            }

            return list;
        }

    }
}
