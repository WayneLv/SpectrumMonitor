using System;
using System.Collections.Generic;
using InstrumentDriver.Core;
using InstrumentDriver.Core.Utility;
using InstrumentDriver.Core.Mock;
using InstrumentDriver.NewInstrument;
using InstrumentDriverCore.Interfaces;

namespace InstrumentDriver.SpectrumMonitor
{
    public enum eDetectorType
    {
        Peak = 0,
        NegPeak,
        Sample,
        Average,
        RMSAverage,//Not support now
    }


    public class SpectrumMonitorInstrument : AbstractInstrument, ISpectrumMonitor
    {
        #region private members

        private readonly PropertyLimits<double> mFrequencyLimits = new PropertyLimits<double>(10e6f, 3000e6f);
        private readonly PropertyLimits<int> mSpectrumFrameCountLimits = new PropertyLimits<int>(256, 65536);
        private readonly PropertyLimits<int> mDPXFrameCountLimits = new PropertyLimits<int>(1024, 65536);
        private readonly PropertyLimits<double> mAttenuationLimits = new PropertyLimits<double>(-100, 50);
        private readonly PropertyLimits<double> mRBWLimits = new PropertyLimits<double>(100f, 10e6f);


        private double mStartFrequency = 100e6;
        private double mStopFrequency = 2500e6;
        private double mAttenuation = 0.0;
        private double mRBW = 1e6;

        private eDetectorType mDisplayDetectorType = eDetectorType.Peak;
        private eDetectorType mInFrameDetector = eDetectorType.Peak;
        private eDetectorType mCrossFrameDetector = eDetectorType.Peak;
        private int mSpectrumFrameCount = 200;

        private int mDPXFrameCount = 200;

        protected enum SpectrumMonitorPropertyChanged : uint
        {
            // Duplicate bits from parent class(es)
            All = CorePropertyChanged.All,

            // Bits used by this class (assign bits from low to high ... parent classes assign high to low)
            FrequencyRange = CorePropertyChanged.Bit0,
            RBW = CorePropertyChanged.Bit1,
            Attenuation = CorePropertyChanged.Bit2,
            SpectrumFrameSetting = CorePropertyChanged.Bit3,
            DPXFrameCount = CorePropertyChanged.Bit4,
            DPXTriggerDensity = CorePropertyChanged.Bit5,

            TDCarrierDetectControl = CorePropertyChanged.Bit6,
            NonTDCarrierDetectControl = CorePropertyChanged.Bit7,
            DDCControl = CorePropertyChanged.Bit8,
            CharacterIdentifyControl = CorePropertyChanged.Bit9,

            TimeStampOffset = CorePropertyChanged.Bit10,
            SpectrumCombineSetting = CorePropertyChanged.Bit11,
            WindowType = CorePropertyChanged.Bit12,

        }

        #endregion

        public SpectrumMonitorInstrument(
            bool simulate = false,
            Dictionary<string, string> options = null)
            :base(options)
        {
            IsSimulated = simulate;

            if (simulate)
            {
                RegDriver = new MockRegDriver();
            }
            else
            {
                RegDriver = new QWorksRegDriver(ErrorLog, simulate);
            }

            // create the registers for this module.
            RegManager = new SpectrumMonitorRegManager(this);


        }

        #region Private methods

        private void UpdateRegValues_FrequencySettings()
        {

            AnyRegSettingsDirty = true;
        }

        private void UpdateRegValues_Attenuation()
        {

            AnyRegSettingsDirty = true;
        }

        private void UpdateRegValues_SpectrumFrameSetting()
        {

            AnyRegSettingsDirty = true;
        }


        #endregion

        #region Common Instrument implementation

        /// <summary>
        /// Returns a reference to the container providing access to all module registers.
        /// </summary>
        internal ReceiverRegisterSet Reg
        {
            get { return (RegManager as SpectrumMonitorRegManager).RecReg; }
        }

        public override string ModelNumber
        {
            get
            {
                return "InstrumentDriver";
            }
        }

        public override string Name
        {
            get
            {
                return "InstrumentDriver";
            }
        }

        public override string SerialNumber
        {
            get
            {
                return "CN0000000001";
            }
        }


        public override void ApplyRegValuesToHw(bool ForceApply)
        {
            RegApply(Reg.Registers, ForceApply);

            ClearDirtyRegSettings();
        }

        public override void InitializeHardware()
        {

        }

        public override void ResetProperties()
        {


        }

        public override void UpdateRegValues(bool ForceApply)
        {
            if (ForceApply)
            {
                SetPropertyChangePending(SpectrumMonitorPropertyChanged.All);
            }

            if (PropertyChangePending(SpectrumMonitorPropertyChanged.FrequencyRange)          
                || PropertyChangePending(SpectrumMonitorPropertyChanged.RBW))
            {
                UpdateRegValues_FrequencySettings();
            }

            if (PropertyChangePending(SpectrumMonitorPropertyChanged.Attenuation))
            {
                UpdateRegValues_Attenuation();
            }

            if (PropertyChangePending(SpectrumMonitorPropertyChanged.SpectrumFrameSetting))
            {
                UpdateRegValues_SpectrumFrameSetting();
            }



            ClearPropertyChangePending(SpectrumMonitorPropertyChanged.All);

        }

        public override void FinishConstruction()
        {

        }


        #endregion

        #region Public Properties
        public double StartFrequency
        {
            get
            {
                return mStartFrequency;
            }

            set
            {
                // Hard limit checking and error reporting...If you don't want the default error (InvalidParameterException) use:
                //     if( ! mExampleValueLimits.IsValid( value ) { ErrorLog.ThrowException( new MyException( ... ) ) }
                // Additional checks (e.g. state dependent limits) can be done in Validate()
                mFrequencyLimits.CheckValidity(value, "StartFrequency", ErrorLog);

                SetIfPropertyChanged(ref mStartFrequency, value, SpectrumMonitorPropertyChanged.FrequencyRange);
            }
        }

        public double StopFrequency
        {
            get
            {
                return mStopFrequency;
            }
            set
            {
                mFrequencyLimits.CheckValidity(value, "StopFrequency", ErrorLog);

                SetIfPropertyChanged(ref mStopFrequency, value, SpectrumMonitorPropertyChanged.FrequencyRange);
            }
        }

        public double Attenuation
        {
            get
            {
                return mAttenuation;
            }
            set
            {
                mFrequencyLimits.CheckValidity(value, "Attenuation", ErrorLog);

                SetIfPropertyChanged(ref mAttenuation, value, SpectrumMonitorPropertyChanged.Attenuation);
            }
        }


        public double RBW
        {
            get
            {
                return mRBW;
            }
            set
            {
                mRBWLimits.CheckValidity(value, "RBW", ErrorLog);
                value = Utility.ClipForRBWSetting(value);
                SetIfPropertyChanged(ref mRBW, value, SpectrumMonitorPropertyChanged.RBW);
            }
        }
        public eDetectorType InFrameDetector
        {
            get
            {
                return mInFrameDetector;
            }
            set
            {
                SetIfPropertyChanged(ref mInFrameDetector, value, SpectrumMonitorPropertyChanged.SpectrumFrameSetting);
            }

        }

        public eDetectorType CrossFrameDetector
        {
            get
            {
                return mCrossFrameDetector;
            }
            set
            {
                SetIfPropertyChanged(ref mCrossFrameDetector, value, SpectrumMonitorPropertyChanged.SpectrumFrameSetting);
            }

        }

        public int SpectrumFrameCount
        {
            get
            {
                return mSpectrumFrameCount;
            }
            set
            {
                mSpectrumFrameCountLimits.CheckValidity(value, "SpectrumFrameCount", ErrorLog);         
                SetIfPropertyChanged(ref mSpectrumFrameCount, value, SpectrumMonitorPropertyChanged.SpectrumFrameSetting);
            }

        }


        public int DPXFrameCount
        {
            get
            {
                return mDPXFrameCount;
            }
            set
            {
                mDPXFrameCountLimits.CheckValidity(value, "DPXFrameCount", ErrorLog);
                SetIfPropertyChanged(ref mDPXFrameCount, value, SpectrumMonitorPropertyChanged.DPXFrameCount);
            }

        }

        public eDetectorType DisplayDetectorType
        {
            get
            {
                return mDisplayDetectorType;
            }
            set
            {
                mDisplayDetectorType = value;
            }

        }

        public double MinFrequency
        {
            get
            {
                return mFrequencyLimits.Minimum;
            }
        }

        public double MaxFrequency
        {
            get
            {
                return mFrequencyLimits.Maximum;
            }
        }

        

        #endregion

        #region Public methods
        public bool ReadSpectrum(ref double[] data, int index = 0)
        {
            //if (IsSimulated)
            {
                data = Utility.SimulateSpectrumData(10000);

                List<ISignalCharacters> sig = new List<ISignalCharacters>();
                ReadSignalCharacter(ref sig);
                return true;
            }
            //else //TODO Read Spectrum from Hardware
            {
                throw new NotImplementedException();
            }
        }


        public bool ReadSignalCharacter(ref List<ISignalCharacters> signalcharacters)
        {
            //if (IsSimulated)
            {
                signalcharacters = Utility.SimulateSignalCharacters();
                return true;
            }
            //else //TODO Read SignaCharacter from Hardware
            {
                throw new NotImplementedException();
            }
        }


        #endregion

        
    }
}
