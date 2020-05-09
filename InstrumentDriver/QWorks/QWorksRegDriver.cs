using System;
using System.CodeDom;
using System.Threading;
using InstrumentDriver.Core.Common.IO;
using InstrumentDriver.Core.Register;
using System.Collections.Generic;
using DeviceFunctionDriver;
using InstrumentDriver.Core.Utility;
using InstrumentDriver.QWorks;

namespace InstrumentDriver.SpectrumMonitor
{
    ////AddressSpace for different address types:
    public enum MyAddressSpace: short
    {
        MappedControlReg = AddressSpace.PxiBar0,
        CarrierReg = AddressSpace.PxiBar1,
        SpectrumDataMemory = AddressSpace.PxiBar2,

    };

    public class QWorksRegDriver : IRegDriver
    {
        private Dictionary<int, string> mBoardNameDictionary = new Dictionary<int, string>()
        {
            {1, "QGF_V5_CPCI "},
            {2, "QGF_V5_CPCIe"},
            {3, "QGF_V5_LAN  "},
            {4, "QGF_V7_CPCI "},
            {5, "QGF_V7_CPCIe"},
            {6, "QGF_V7_LAN  "},
            {7, "QNF_V2_CPCI "},
            {8, "QNF_V2_CPCIe"},
            {9, "QNF_V2_SFP  "},
            {10, "QGF_V2_HPC  "}
        };

        private uint mDeviceID = 0;
        private IErrorLog mErrorLog;
        private string mBoardName = "None";

        public QWorksRegDriver(IErrorLog errorLog, bool simulate = false)
        {
            mErrorLog = errorLog;
            mSimulate = simulate;

            QWorksInitialize();
        }

        private void QWorksInitialize()
        {
            uint version = 0;
            Qworks.Version(ref version);
            mErrorLog.AddInformation(string.Format("Qworks version = {0}", version));

            uint devnum = 0;
            int status = Qworks.Initialize(ref devnum);
            mErrorLog.AddInformation(string.Format("Initialize, status = {0}", status));
            mErrorLog.AddInformation(string.Format("Initialize devsum = {0}", devnum));
            if (devnum < 1)
            {
                mErrorLog.AddError("Qworks initialize error");
            }
            else
            {
                //Convert devum to device ID, the device ID should be the bit position
                uint devcnt = 0;
                do
                {
                    if ((devnum & 0x01) == 1)//Get a board 
                    {
                        int port = Qworks.GetPort(devcnt);
                        mErrorLog.AddInformation(string.Format("Device number {0}, port = {1}", devcnt, port));
                        if (mBoardNameDictionary.ContainsKey(port))
                        {
                            mBoardName = mBoardNameDictionary[port];
                        }
                        else
                        {
                            mBoardName = "Unknown";
                        }

                        if (port != 5)
                        {
                            //Now we just support QGF_V7_CPCIe
                            mErrorLog.AddInformation(string.Format("Device number {0} is a {1} board", devcnt, mBoardName));
                        }
                        else
                        {
                            mErrorLog.AddInformation(string.Format("{0} carrier board detected, device number = {1}", mBoardName, devcnt));
                            mDeviceID = devcnt;
                        }
                    }

                    devcnt++;
                    devnum = devnum >> 1;
                } while (devnum != 0);

            }
        }

        public ISession ActiveSession
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        private AddressSpace mAddressSpace = AddressSpace.PxiBar0;
        public AddressSpace AddressSpace
        {
            get
            {
                return mAddressSpace;
            }

            set
            {
                mAddressSpace = value;
            }
        }

        public int InternalBAR
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsRecordingSession
        {
            get
            {
                return false;
            }
        }

        public Mutex Resource
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ISession[] Sessions
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public void ArrayRead(int barOffset, ref int[] data32, int startIndex, int num32BitWords)
        {
            ArrayRead32((AddressSpace)MyAddressSpace.MappedControlReg, barOffset, ref data32, startIndex, num32BitWords);
        }

        public void ArrayRead(int barOffset, ref byte[] data8, int startIndex, int numBytes)
        {
            throw new NotImplementedException();
        }

        public void ArrayRead(AddressSpace bar, int barOffset, ref byte[] data8, int startIndex, int numBytes)
        {
            throw new NotImplementedException();
        }

        public int[] ArrayRead32(int barOffset, int num32BitWords)
        {
            return ArrayRead32((AddressSpace) MyAddressSpace.MappedControlReg, barOffset, num32BitWords);
        }

        public int[] ArrayRead32(AddressSpace bar, int barOffset, int num32BitWords)
        {
            int [] readData = new int[num32BitWords];
            ArrayRead32(bar, barOffset, ref readData, 0, num32BitWords);
            return readData;
        }

        public byte[] ArrayRead8(int barOffset, int numBytes)
        {
            throw new NotImplementedException();
        }

        public byte[] ArrayRead8(AddressSpace bar, int barOffset, int numBytes)
        {
            throw new NotImplementedException();
        }

        private void ArrayRead32(AddressSpace bar, int barOffset, ref int[] data32, int startIndex, int num32BitWords)
        {
            if (data32.Length < (startIndex + num32BitWords))
            {
                throw new Exception("Wrong ArrayRead32 : data32.Length < (startIndex + num32BitWords)");
            }

            for (int i = startIndex; i < (startIndex + num32BitWords); i++)
            {
                data32[i] = RegRead(bar, barOffset + i);
            }
        }

        public void ArrayWrite(int barOffset, int[] data32)
        {
            ArrayWrite((AddressSpace)MyAddressSpace.MappedControlReg, barOffset, data32, data32.Length, 0);
        }

        public void ArrayWrite(int barOffset, byte[] data8)
        {
            throw new NotImplementedException();
        }

        public void ArrayWrite(int barOffset, int[] data32, int length)
        {
            ArrayWrite((AddressSpace)MyAddressSpace.MappedControlReg, barOffset, data32, length, 0);
        }

        public void ArrayWrite(int barOffset, byte[] data8, int startByte, int numBytes)
        {
            throw new NotImplementedException();
        }

        public void ArrayWrite(AddressSpace bar, int barOffset, int[] data32, int length)
        {
            ArrayWrite(bar,  barOffset, data32,  length,0);
        }

        public void ArrayWrite(int barOffset, int[] data32, int length, int offset)
        {
            ArrayWrite((AddressSpace) MyAddressSpace.MappedControlReg, barOffset, data32, length, offset);
        }

        public void ArrayWrite(AddressSpace bar, int barOffset, int[] data32, int length, int offset)
        {
            if (data32.Length < (offset + length))
            {
                throw new Exception("Wrong ArrayWrite: offset + length > size");
            }
            for (int i = offset; i < (offset + length); i++)
            {
                int val = data32[i];
                RegWrite(bar,barOffset + i,val);
            }
        }

        public void ArrayWrite(AddressSpace bar, int barOffset, byte[] data8, int startIndex, int numBytes)
        {
            throw new NotImplementedException();
        }

        public void BeginBuffering()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            Qworks.Close();
        }

        public void EndBuffering()
        {
            throw new NotImplementedException();
        }

        public void FifoWrite(AddressSpace PxiBar, int[] data32)
        {
            throw new NotImplementedException();
        }

        public void FifoWrite(AddressSpace PxiBar, int[] data32, int length)
        {
            throw new NotImplementedException();
        }

        public void ReadFifo(AddressSpace bar, int barOffset, int length, byte[] data)
        {
            throw new NotImplementedException();
        }

        public void ReadFifo(AddressSpace bar, int barOffset, int length, int[] data)
        {
            throw new NotImplementedException();
        }

        public void ReadFifo(AddressSpace bar, int barOffset, int length, byte[] data, int offset)
        {
            throw new NotImplementedException();
        }

        public void ReadFifo(AddressSpace bar, int barOffset, int length, int[] data, int offset)
        {
            throw new NotImplementedException();
        }

        public int RegRead(int barOffset)
        {
            return RegRead((AddressSpace)MyAddressSpace.MappedControlReg, barOffset);
        }

        public int RegRead(AddressSpace space, int barOffset)
        {
            int val = 0;
            try
            {
                MyAddressSpace addressSpace = (MyAddressSpace) space;
                switch (addressSpace)
                {
                    case MyAddressSpace.MappedControlReg:
                        val = RegReadWrap(barOffset);
                        break;
                    case MyAddressSpace.CarrierReg:
                        val = (int) RegAccessWrapper.QWorksRegRead((uint) barOffset, mDeviceID);
                        break;
                    case MyAddressSpace.SpectrumDataMemory:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception ex)
            {
                mErrorLog.AddError(String.Format("RegRead Error: Type={0},Offset={1}: {2}", (MyAddressSpace)space, barOffset, ex.Message));
            }

            return val;
        }

        public long RegRead64(int barOffset)
        {
            throw new NotImplementedException();
        }

        public long RegRead64(AddressSpace space, int barOffset)
        {
            throw new NotImplementedException();
        }

        public void RegRefresh(IRegister[] regArray)
        {
            throw new NotImplementedException();
        }

        public void RegRefresh(IRegister[] regArray, int startIndex, int endIndex)
        {
            throw new NotImplementedException();
        }

        public void RegWrite(int barOffset, long value)
        {
            throw new NotImplementedException();
        }

        public void RegWrite(int barOffset, int value)
        {
            RegWrite((AddressSpace)MyAddressSpace.MappedControlReg, barOffset, value);
        }

        public void RegWrite(AddressSpace space, int barOffset, long value)
        {
            throw new NotImplementedException();
        }

        public void RegWrite(AddressSpace space, int barOffset, int value)
        {
            try
            {
                MyAddressSpace addressSpace = (MyAddressSpace) space;
                switch (addressSpace)
                {
                    case MyAddressSpace.MappedControlReg:
                        RegWriteWrap(barOffset, value);
                        break;
                    case MyAddressSpace.CarrierReg:
                        RegAccessWrapper.QWorksRegWrite((uint) value, (uint) barOffset, mDeviceID);
                        break;
                    case MyAddressSpace.SpectrumDataMemory:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception ex)
            {
                mErrorLog.AddError(String.Format("RegWrite Error: Type={0},Offset={1},Value={2}: {3}", (MyAddressSpace)space, barOffset,value,ex.Message));
            }
        }

        public ISession Session(AddressSpace bar)
        {
            throw new NotImplementedException();
        }

        public void VisaTimeOut(int msTimeout)
        {
            throw new NotImplementedException();
        }

        public void VisaTimeOut(AddressSpace bar, int msTimeout)
        {
            throw new NotImplementedException();
        }

        public void VisaTimeOutDefault()
        {
            throw new NotImplementedException();
        }

        public void VisaTimeOutDefault(AddressSpace bar)
        {
            throw new NotImplementedException();
        }


        //Wrap the register read/write by using the driver

        private bool mSimulate = true;
        private Dictionary<int, int> mSimulateRegDict = new Dictionary<int, int>();
        private void RegWriteWrap(int offset, int value)
        {
            //Wrap the register to correct value and format

            if (mSimulate)
            {
                if (mSimulateRegDict.ContainsKey(offset))
                    mSimulateRegDict[offset] = value;
                else
                    mSimulateRegDict.Add(offset, value);
            }
            else
            {
                RegAccessWrapper.Write( mDeviceID,(uint)offset,(uint)value);
            }
        }

        private int RegReadWrap(int offset)
        {
            int readvalue = 0;
            if (mSimulate)
            {
                if (mSimulateRegDict.ContainsKey(offset))
                    readvalue = mSimulateRegDict[offset];
            }
            else
            {
                readvalue = (int)RegAccessWrapper.Read(mDeviceID, (uint)offset);
            }

            //Get the real value of the register 
            return readvalue;
        }

        public Dictionary<string, string> GetDeviceInfo()
        {
            const float MAX_VOL_CUR = 999; //Max value of Voltage and current, if read value is larger than it, then it is meaningless
            Dictionary<string, string> deviceInfo = new Dictionary<string, string>();

            deviceInfo.Add("Board Name",mBoardName);

            uint readInfo = 0;
            //F0 Info
            Qworks.F0Info(ref readInfo, (uint)EnumF0Info.F0Info_Elf, mDeviceID);
            deviceInfo.Add("F0 ELF Version", F0VersionString(readInfo));
            Qworks.F0Info(ref readInfo, (uint)EnumF0Info.F0Info_Bit, mDeviceID);
            deviceInfo.Add("F0 bit Version", F0VersionString(readInfo));

            //F1 Info
            Qworks.F1Info(ref readInfo, (uint)EnumF1Info.F1Info_Version, mDeviceID);
            deviceInfo.Add("F1 Version", F1VersionString(readInfo));
            Qworks.F1Info(ref readInfo, (uint)EnumF1Info.F1Info_StandingTime, mDeviceID);
            deviceInfo.Add("F1 Run Time", F1RunTimeString(readInfo));
            Qworks.F1Info(ref readInfo, (uint)EnumF1Info.F1Info_ClockFreq, mDeviceID);
            deviceInfo.Add("F1 Clock Rate", string.Format("{0:F1} (MHz)",(float)readInfo * 1.024 / 1000));
            Qworks.F1Info(ref readInfo, (uint)EnumF1Info.F1Info_BitState, mDeviceID);
            deviceInfo.Add("F1 Bit State", (readInfo == 1) ? "Loaded" : "Unloaded");
            Qworks.F1Info(ref readInfo, (uint)EnumF1Info.F1Info_F1F0Link, mDeviceID);
            deviceInfo.Add("F0 and F1 Link", (readInfo == 1)? "Linked":"Unlinked");

            //F1 Ram Size
            Qworks.F1RamSize(ref readInfo, mDeviceID);
            deviceInfo.Add("F1 RAM Capacity", string.Format("{0:F1} (kB)",(float)readInfo/1024));

            //Sensor Info
            SensorInfo sensorInfo = new SensorInfo();
            Qworks.SensorInfo(ref sensorInfo, mDeviceID);
            deviceInfo.Add("F0 VCC INT", string.Format("{0:F2} V", sensorInfo.F0VCCINT > MAX_VOL_CUR? 0.0 : sensorInfo.F0VCCINT));
            deviceInfo.Add("F0 VCC AUX", string.Format("{0:F2} V", sensorInfo.F0VCCAUX > MAX_VOL_CUR ? 0.0 : sensorInfo.F0VCCAUX));
            deviceInfo.Add("F0 Temperature", string.Format("{0:F2} C", sensorInfo.F0Temperature));
            deviceInfo.Add("F1 VCC INT", string.Format("{0:F2} V(Max {1:F2} V)",
                sensorInfo.F1VCCINT > MAX_VOL_CUR ? 0.0 : sensorInfo.F1VCCINT, sensorInfo.F1VCCINTMAX > MAX_VOL_CUR ? 0.0 : sensorInfo.F1VCCINTMAX));
            deviceInfo.Add("F1 VCC AUX", string.Format("{0:F2} V(Max {1:F2} V)", 
                sensorInfo.F1VCCAUX > MAX_VOL_CUR ? 0.0 : sensorInfo.F1VCCAUX, sensorInfo.F1VCCAUXMAX > MAX_VOL_CUR ? 0.0 : sensorInfo.F1VCCAUXMAX));
            deviceInfo.Add("F1 Temperature", string.Format("{0:F2} C",sensorInfo.F1Temperature));

            deviceInfo.Add("F1 Temperature Max", string.Format("{0:F2} C", sensorInfo.F1TemperatureMax));
            deviceInfo.Add("Voltage 12V", string.Format("{0:F2} V", sensorInfo.QGFVoltage12 > MAX_VOL_CUR ? 0.0 : sensorInfo.QGFVoltage12));
            deviceInfo.Add("Current of 12V", string.Format("{0:F2} A", sensorInfo.QGFCurrent12 > MAX_VOL_CUR ? 0.0 : sensorInfo.QGFCurrent12));
            deviceInfo.Add("Voltage 5V", string.Format("{0:F2} V", sensorInfo.QGFVoltage5 > MAX_VOL_CUR ? 0.0 : sensorInfo.QGFVoltage5));
            deviceInfo.Add("Current of 5V", string.Format("{0:F2} A", sensorInfo.QGFCurrent5 > MAX_VOL_CUR ? 0.0 : sensorInfo.QGFCurrent5));
            deviceInfo.Add("Temperature 0", string.Format("{0:F2} C", sensorInfo.QGFTemperature0));
            deviceInfo.Add("Temperature 1", string.Format("{0:F2} C", sensorInfo.QGFTemperature1));
            deviceInfo.Add("Temperature 2", string.Format("{0:F2} C", sensorInfo.QGFTemperature2));
            deviceInfo.Add("Temperature 3", string.Format("{0:F2} C", sensorInfo.QGFTemperature3));


            return deviceInfo;
        }

        private string F0VersionString(uint value)
        {
            //输出数据解析方式, 年:[31:24] + 2000, 月:[23:16], 日[15:8], 时[7:0].
            uint hourMask = 0xff;
            uint dayMask = 0xff00;
            uint monthMask = 0xff0000;
            uint yearMask = 0xff000000;

            return String.Format("{0}-{1}-{2}-{3}", ((value & yearMask) >> 24) + 2000, (value & monthMask)>>16, (value & dayMask)>>8,
                value & hourMask);

        }

        private string F1VersionString(uint value)
        {
            //输出数据解析方式, 日:[31:27], 月:[26:23], 年[22:17] + 2000, 时[16:12], 分:[11:6], 秒[5:0].
            uint secondMask = 0x3f;
            uint minMask = 0xfc0;
            uint hourMask = 0x1f000;
            uint yearMask = 0x7e0000;
            uint monMask = 0x7800000;
            uint dayMask = 0xF8000000;

            return String.Format("{0}-{1}-{2} {3}:{4}:{5}", ((value & yearMask)>>17) + 2000, (value & monMask)>>23, (value & dayMask)>>27,
                (value & hourMask)>>12, (value & minMask)>>6, value & secondMask);
        }

        private string F1RunTimeString(uint value)
        {
            //输出数据解析方式, 时:[16:12], 分[11:6], 秒[5:0].
            uint secondMask = 0x3f;
            uint minMask = 0xfc0;
            uint hourMask = 0x1f000;

            return String.Format("{0}:{1}:{2}", (value & hourMask) >> 12, (value & minMask) >> 6, value & secondMask);
        }


    }
}
