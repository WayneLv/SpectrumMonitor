using DeviceFunctionDriver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstrumentDriver.Core.Utility;

namespace InstrumentDriver.QWorks
{
    public class RegAccessWrapper
    {
        private static readonly uint TOTAL_REG_NUM = 64;

        private static readonly int REG_WRITE_MAX_LENGTH = 30;
        private static readonly uint REG_WRITE_BASEADDR = 4;

        private static readonly int REG_READ_MAX_LENGTH = 30;
        private static readonly uint REG_READ_BASEADDR = 34;

        private static readonly uint CONTROL_ADDR = 0;
        private static readonly uint STATUS_ADDR = 1;
        private static readonly uint STARTOFFSET_ADDR = 2;

        private static readonly int CONTROL_MODE_WRITE = 1;
        private static readonly int CONTROL_MODE_READ = 0;
        private static readonly int CONTROL_REFRESH_BIT = 7; //bit 7
        private static readonly int CONTROL_MODE_BIT = 8; //bit 8
        private static readonly int CONTROL_LENGTH_BIT = 9; //bit 9~13

        private static readonly Dictionary<int,string> mErrorInfoDictionary = new Dictionary<int, string>()
        {
            {0,  "Err_Success"},
            {-1, "Err_Error"},
            {-2, "Err_DriverOpen"},
            {-3, "Err_ScanDevices"},
            {-4, "Err_GetDeviceInfo "},
            {-5, "Err_DeviceOpen"},
            {-6, "Err_DmaBufLock"},
            {-7, "Err_PortNoFunc"},
            {-8, "Err_Port"},
            {-9, "Err_Switch"},
            {-10, "Err_ParaRange"},
            {-11, "Err_CmdRange"},
            {-12, "Err_DevNum"},
            {-13, "Err_Interrupt"},
            {-14, "Err_BitState"},
            {-15, "Err_BitF0F1Link"},
            {-16, "Err_HQNFBusy"},
            {-17, "Err_HQNFFree"},
            {-18, "Err_SocketInit"},
            {-19, "Err_SocketBind"},
            {-20, "Err_SocketConnect"},
            {-21, "Err_SocketSend"},
            {-22, "Err_SocketRecv"},
            {-23, "Err_Busy"},
            {-24, "Err_HQNFControl"},
            {-25, "Err_TimeOut"},
            {-26, "Err_Mutex"}
        };


        public static string ErrorInfo(int code)
        {
            if (mErrorInfoDictionary.ContainsKey(code))
                return mErrorInfoDictionary[code];
            else
            {
                return "Unknown error";
            }

        }

        public static void QWorksRegWrite(uint regdata, uint regnum, uint devnum)
        {
            int status = Qworks.F1WriteReg(regdata, regnum, devnum);
            if (status < 0)
            {
                throw new Exception(string.Format("QWorksRegWrite Error: {0}", ErrorInfo(status)));
            }
        }

        public static uint QWorksRegRead(uint regnum, uint devnum)
        {
            uint readvalue = 0;
            int status = Qworks.F1ReadReg(ref readvalue, regnum, devnum);
            if (status < 0)
            {
                throw new Exception(string.Format("QWorksRegRead Error: {0}", ErrorInfo(status)));
            }
            return readvalue;
        }

        private static void AccessRefresh(uint device, int writeOrRead, int length = 1)
        {
            int controlvalue = (length << CONTROL_LENGTH_BIT) | (writeOrRead << CONTROL_MODE_BIT) | (1 << CONTROL_REFRESH_BIT);
            QWorksRegWrite((uint)controlvalue, CONTROL_ADDR, device);
            controlvalue &= (~(1 << CONTROL_REFRESH_BIT));
            QWorksRegWrite((uint)controlvalue, CONTROL_ADDR, device);
        }

        public static void Write(uint device, uint offset, uint value)
        {
            QWorksRegWrite(offset, STARTOFFSET_ADDR, device);
            QWorksRegWrite(value, REG_WRITE_BASEADDR, device);
            AccessRefresh(device, CONTROL_MODE_WRITE);
        }

        public static uint Read(uint device, uint offset)
        {
            QWorksRegWrite(offset, STARTOFFSET_ADDR, device);
            AccessRefresh(device, CONTROL_MODE_READ);
            return QWorksRegRead(REG_READ_BASEADDR, device);
        }

        public static void WriteArray(uint device, uint startoffset, uint[] values)
        {
            int remaininglen = values.Length;
            uint currentoffset = startoffset;
            while (remaininglen > 0) //write maximum REG_WRITE_MAX_LENGTH length
            {
                int writelen = (remaininglen < REG_WRITE_MAX_LENGTH) ? remaininglen : REG_WRITE_MAX_LENGTH;
                QWorksRegWrite(currentoffset, STARTOFFSET_ADDR, device);

                for (int i = 0; i < writelen; i++)
                {
                    QWorksRegWrite(values[i + (currentoffset - startoffset)], REG_WRITE_BASEADDR + (uint)i, device);
                }

                AccessRefresh(device, CONTROL_MODE_WRITE, writelen);

                remaininglen -= writelen;
                currentoffset += (uint)writelen;
            }
        }

        public static uint[] ReadArray(uint device, uint startoffset,int length)
        {
            if (length <= 0)
            {
                throw new Exception("Array read length must be larger than 0!");
            }

            uint[] retvalues = new uint[length];

            int remaininglen = length;
            uint currentoffset = startoffset;
            while (remaininglen > 0) //write maximum REG_WRITE_MAX_LENGTH length
            {
                int readlen = (remaininglen < REG_READ_MAX_LENGTH) ? remaininglen : REG_READ_MAX_LENGTH;
                QWorksRegWrite(currentoffset, STARTOFFSET_ADDR, device);
                AccessRefresh(device, CONTROL_MODE_READ, readlen);

                for (int i = 0; i < readlen; i++)
                {
                    retvalues[i + (currentoffset - startoffset)] = QWorksRegRead(REG_READ_BASEADDR + (uint)i, device);
                }

                remaininglen -= readlen;
                currentoffset += (uint)readlen;
            }

            return retvalues;
        }

    }
}
