using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstrumentDriverCore.Interfaces
{
    public interface ISpectrumMonitor
    {
        double StartFrequency { get; set; }

        double StopFrequency { get; set; }

        double MinFrequency { get;}

        double MaxFrequency { get;}

        bool ReadSpectrum(ref double[] data, int index = 0);

        bool ReadSignalCharacter(ref List<ISignalCharacters> signalcharacters);

        bool ReadDpxData(out int[,] dpxData);
    }
}
