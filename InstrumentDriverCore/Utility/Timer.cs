/******************************************************************************
 *                                                                         
 *               Copyright 2011 .
 *               All rights reserved.
 *
 *****************************************************************************/

using System.Diagnostics;
using System.Threading;

namespace InstrumentDriver.Core.Utility
{
    public class Timer
    {
        /// <summary>
        /// Use the high performance counters (via Stopwatch) to implement
        /// a spin-loop wait.  This method should only be used for delays of
        /// a few milliseconds or less.
        /// </summary>
        /// <param name="delaySeconds">delay in seconds</param>
        public static void SpinDelay( double delaySeconds )
        {
            if( delaySeconds > 0 )
            {
                double minTicks = Stopwatch.Frequency * delaySeconds;
                Stopwatch timer = new Stopwatch();
                timer.Start();
                while( timer.ElapsedTicks < minTicks )
                {
                    Thread.Sleep( 0 );
                }
                timer.Stop();
            }
        }
    }
}