using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstrumentDriverCore.Interfaces
{
    public interface ISignalCharacters
    {
        double Frequency
        {
            get;
        }

        double BandWidth
        {
            get;
        }

        double Power
        {
            get;
        }

        bool IsTDSignal
        {
            get;
        }

    }
}
