using InstrumentDriverCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstrumentDriver.FunctionSupport
{
    public class SignalCharacters : ISignalCharacters
    {
        private double mFrequency = 1e9;
        private double mBandWidth = 10e6;
        private bool mIsTDSignal = false;
        private double mPower = -100;

        public SignalCharacters(double freq,double bw,double power,bool isTD)
        {
            mFrequency = freq;
            mBandWidth = bw;
            mIsTDSignal = isTD;
            mPower = power;
        }

        public double Frequency
        {
            get
            {
                return mFrequency;
            }
        }

        public double BandWidth
        {
            get
            {
                return mBandWidth;
            }
        }

        public bool IsTDSignal
        {
            get
            {
                return mIsTDSignal;
            }
        }

        public double Power
        {
            get
            {
                return mPower;
            }
        }
    }
}
