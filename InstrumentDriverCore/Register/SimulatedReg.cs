using InstrumentDriver.Core.Utility.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstrumentDriver.Core.Register
{
    public class SimulatedReg : Reg64
    {
        private Int64 mSimulatedHardware = 0;

        public SimulatedReg(string name, Int32 offset, Type bfType, IRegDriver driver)
            : base(name, offset, bfType, driver, RegType.RW)
        {
        }

        public SimulatedReg(string name,
                      Int32 offset,
                      Type bfType,
                      IRegDriver driver,
                      RegType eRegType)
            : base(name, offset, bfType, driver, eRegType)
        {

        }

        public override long DriverRead()
        {
            // ignore the driver ... this is always simulated...
            var regVal = mSimulatedHardware;

            if (mLogger.IsLoggingEnabledFor(LogLevel.Fine))
            {
                mLogger.LogAppend(new RegisterLoggingEvent(LogLevel.Fine, regVal, this)
                { Operation = RegisterLoggingEvent.OperationType.RegRd });
            }

            return regVal;
        }

        public override void DriverWrite(IRegDriver driver)
        {
            if (mLogger.IsLoggingEnabledFor(LogLevel.Fine))
            {
                mLogger.LogAppend(new RegisterLoggingEvent(LogLevel.Fine, mValue, this)
                {
                    Operation = (driver == null || driver.IsRecordingSession == false)
                                        ? RegisterLoggingEvent.OperationType.RegWr
                                        : RegisterLoggingEvent.OperationType.RegWrCS
                });
            }
            // ignore the driver ... this is always simulated
            mSimulatedHardware = mValue;
        }

        public new static IRegister ConstructReg(string name,
                                                      RegDef regDef,
                                                      IRegDriver driver,
                                                      int baseAddr,
                                                      object[] args)
        {
            var newRegister = new SimulatedReg(name, regDef.BARoffset + baseAddr, regDef.BFenum, driver,
                                                         regDef.RegType);
            return newRegister;
        }

        public new static void RegisterTypeWithFactory()
        {
            RegFactory.RegisterType("SimulatedReg", typeof(SimulatedReg));
        }


    }
}
