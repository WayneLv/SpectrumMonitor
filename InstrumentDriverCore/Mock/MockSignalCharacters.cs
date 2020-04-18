using InstrumentDriverCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstrumentDriverCore.Mock
{
    class MockSignalCharacters : ISignalCharacters
    {
        private double mFrequency = 1e9;
        private double mBandWidth = 10e6;
        private bool mIsTDSignal = false;
        private double mPower = -100;
        private static Random mSeedRd = new Random();

        public MockSignalCharacters()
        {
            Random Rd = new Random(mSeedRd.Next());
            mFrequency = Rd.Next(100,5900) * 1e6;
            mBandWidth = Rd.Next(1, 200) * 1e6;
            mIsTDSignal = Rd.Next(0, 2) == 0 ? false : true;
            mPower = Rd.Next(-8000, 1500) / 100.0;
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
