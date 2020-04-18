using InstrumentDriver.Core.Interfaces;
using InstrumentDriver.Core.Register;
using InstrumentDriver.NewInstrument;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstrumentDriver.SpectrumMonitor
{


    public class SpectrumMonitorRegManager : RegManager
    {

        private ReceiverRegisterSet mRecReg = null;
        private SourceRegisterSet mSrcReg = null;

        public SpectrumMonitorRegManager(IInstrument module) : base()
        {
            mRecReg = new ReceiverRegisterSet(this, module, "ReceiverReg");
            mSrcReg = new SourceRegisterSet(this, module, "SourceReg");
            new ReceiverDspRegisterSet(this, module, "ReceiverDsp");
            new SourceDspRegisterSet(this, module, "SourceDsp");
            new SoftwareLatchesRegisterSet(this, module, "SoftwareLatches");

            foreach (var item in GetRegisterSets())
            {
                item.SetInitialValues();
                module.RegApply(item.Registers, true);
            }
        }

        public ReceiverRegisterSet RecReg
        {
            get { return mRecReg; }
        }

        public SourceRegisterSet SrcReg
        {
            get { return mSrcReg; }
        }
    }
}
