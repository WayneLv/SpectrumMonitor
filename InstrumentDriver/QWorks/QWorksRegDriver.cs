using System;
using System.Threading;
using InstrumentDriver.Core.Common.IO;
using InstrumentDriver.Core.Register;
using System.Collections.Generic;
using DeviceFunctionDriver;
using InstrumentDriver.Core.Utility;

namespace InstrumentDriver.SpectrumMonitor
{
    public class QWorksRegDriver : IRegDriver
    {
        private uint mDeviceID = 0;

        public QWorksRegDriver()
        {
            Qworks.Initialize(ref mDeviceID);
            if (mDeviceID < 1)
            {
                
            }

            uint version = 0;
            Qworks.Version(ref version);


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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public int[] ArrayRead32(AddressSpace bar, int barOffset, int num32BitWords)
        {
            throw new NotImplementedException();
        }

        public byte[] ArrayRead8(int barOffset, int numBytes)
        {
            throw new NotImplementedException();
        }

        public byte[] ArrayRead8(AddressSpace bar, int barOffset, int numBytes)
        {
            throw new NotImplementedException();
        }

        public void ArrayWrite(int barOffset, int[] data32)
        {
            throw new NotImplementedException();
        }

        public void ArrayWrite(int barOffset, byte[] data8)
        {
            throw new NotImplementedException();
        }

        public void ArrayWrite(int barOffset, int[] data32, int length)
        {
            throw new NotImplementedException();
        }

        public void ArrayWrite(int barOffset, byte[] data8, int startByte, int numBytes)
        {
            throw new NotImplementedException();
        }

        public void ArrayWrite(AddressSpace bar, int barOffset, int[] data32, int length)
        {
            throw new NotImplementedException();
        }

        public void ArrayWrite(int barOffset, int[] data32, int length, int offset)
        {
            throw new NotImplementedException();
        }

        public void ArrayWrite(AddressSpace bar, int barOffset, int[] data32, int length, int offset)
        {
            throw new NotImplementedException();
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
            return RegRead(AddressSpace.PxiBar0, barOffset);
        }

        public int RegRead(AddressSpace space, int barOffset)
        {
            return RegReadWrap(barOffset);
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
            throw new NotImplementedException();
        }

        public void RegWrite(AddressSpace space, int barOffset, long value)
        {
            throw new NotImplementedException();
        }

        public void RegWrite(AddressSpace space, int barOffset, int value)
        {
            RegWriteWrap(barOffset, value);
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

            }


            //Get the real value of the register 

            return readvalue;
        }
    }
}
