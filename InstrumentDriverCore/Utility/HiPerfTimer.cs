/******************************************************************************
 *                                                                         
 *               Copyright 2011 .
 *               All rights reserved.
 *
 *****************************************************************************/

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace InstrumentDriver.Core.Utility
{
    public class HiPerfTimer
    {
        [DllImport( "Kernel32.dll" )]
        private static extern bool QueryPerformanceCounter( out long lpPerformanceCount );

        [DllImport( "Kernel32.dll" )]
        private static extern bool QueryPerformanceFrequency( out long lpFrequency );


        private static double dFreq = 0;
        private static double dClockPer_mS = 0;

        private static long m_dLastTS = 0;

        /// <summary>
        /// Gets a Time Stamp from the High Performance Timer.
        /// </summary>
        public static long TimeStamp
        {
            get
            {
                long TS;
                QueryPerformanceCounter( out TS );
                return TS;
            }
        }

        /// <summary>
        /// Marks a Time Stamp for using <see cref="ElapsedTime()"/>.
        /// </summary>
        public static void MarkTime()
        {
            m_dLastTS = HiPerfTimer.TimeStamp;
        }

        /// <summary>
        /// Each time you call this it returns seconds since last time you called <see cref="MarkTime"/>.
        /// </summary>
        /// <returns>elapsed time in seconds.</returns>
        /// <remarks> Must call <see cref="MarkTime"/> once before the first call to this 
        /// function.</remarks>
        public static double ElapsedTime()
        {
            long newTS = HiPerfTimer.TimeStamp;
            return ElapsedTime( m_dLastTS, newTS );
        }

        /// <summary>
        /// Returns the elapsed in seconds since the supplied time stamp.
        /// </summary>
        /// <param name="TS">timestamp marking the start of elapsed time.</param>
        /// <returns>elapsed time in seconds.</returns>
        public static double ElapsedTime( long TS )
        {
            long newTS = HiPerfTimer.TimeStamp;
            return ElapsedTime( TS, newTS );
        }

        public static bool ElaspedTimeHasOccurred( long TS, double elapsedTime_sec )
        {
            double elapsedTime = ElapsedTime( TS );
            return ( elapsedTime >= elapsedTime_sec ) ? true : false;
        }

        public static void BusyWait( double dlapsedTime_sec )
        {
            long TS = HiPerfTimer.TimeStamp;
            while( !HiPerfTimer.ElaspedTimeHasOccurred( TS, dlapsedTime_sec ) )
            {
                Thread.Sleep( 0 ); // make sure other threads can run
            }
        }


        /// <summary>
        /// Each time you call this it returns seconds since last time you called <see cref="MarkTime"/>.
        /// </summary>
        /// <returns>elapsed time in milliseconds.</returns>
        /// <remarks> Must call <see cref="MarkTime"/> once before the first call to this 
        /// function.</remarks>
        public static double ElapsedTime_mS()
        {
            long newTS = HiPerfTimer.TimeStamp;
            return ElapsedTime_mS( m_dLastTS, newTS );
        }


        /// <summary>
        /// Returns times in seconds between two Time Stamps.
        /// </summary>
        /// <param name="startTS">staring Time Stamp.</param>
        /// <param name="endTS">Ending Time Stamp.</param>
        /// <returns>seconds</returns>
        public static double ElapsedTime( long startTS, long endTS )
        {
            if( dFreq == 0 )
            {
                GetClockFreq();
            }
            return (double)( endTS - startTS ) / dFreq;
        }

        /// <summary>
        /// returns the elapsed time in seconds between two time stamps.
        /// </summary>
        /// <param name="startTS">starting time stamp.</param>
        /// <param name="endTS">ending timestamp.</param>
        /// <returns>elapsed time in seconds.</returns>
        public static double ElapsedTime_mS( long startTS, long endTS )
        {
            if( dFreq == 0 )
            {
                GetClockFreq();
            }

            return (double)( endTS - startTS ) * dClockPer_mS;
        }

        private static double GetClockFreq()
        {
            long lFreq;
            if( QueryPerformanceFrequency( out lFreq ) == false )
            {
                throw new ApplicationException( "High FreqTimer does not exist." );
            }
            dFreq = (double)lFreq;
            dClockPer_mS = 1000 / dFreq;

            return dFreq;
        }

        private static double ClockPeriod_nS
        {
            get
            {
                return 1.0e6 * dClockPer_mS;
            }
        }
    }
}