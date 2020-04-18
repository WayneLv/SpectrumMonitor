using System;
using System.Text;
using InstrumentDriver.Core.Common.IO;
using InstrumentDriver.Core.Utility.Log;

namespace InstrumentDriver.Core.Register
{
    public class EigerBuffer32 : Buffer32
    {
        public EigerBuffer32( string name, AddressSpace BAR, int barOffset, int sizeInBytes, IRegDriver driver )
            : base( name, BAR, barOffset, sizeInBytes, driver )
        {
        }

        public EigerBuffer32( string name, int barOffset, int sizeInBytes, IRegDriver driver )
            : base( name, barOffset, sizeInBytes, driver )
        {
        }

        /// <summary>
        /// The Eiger Slug Flash buffer is a 512 32-bit word sized buffer that is 
        /// mapped to a 2 32-bit register space.
        /// The first register is used to send the FPGA command to reset pointer.
        /// The second register is used to write data (512 words, one word at a time).  
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startIndex"></param>
        /// <param name="length">length in bytes??</param>
        /// <param name="statusRegister">Status register to poll</param>
        public bool Write( byte[] data, int startIndex, int length, IRegister statusRegister )
        {
            //if( !Name.Contains( "Slug" ) ) // To do: Replace magic word
            //{
            //    base.Write( data, startIndex, length );
            //}
            //else
            {
                if( length > mBufSizeInBytes )
                {
                    throw new ApplicationException( REQUESTED_SIZE_TOO_LARGE );
                }

                if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
                {
                    mLogger.LogAppend( new RegisterLoggingEvent( LogLevel.Fine, length, this )
                        {
                            Operation = RegisterLoggingEvent.OperationType.BufWr8s
                        } );
                    if( mLogger.IsLoggingEnabledFor( LogLevel.Finest ) )
                    {
                        LogBuffer8( "WRITE Buffer8[]=", data, length );
                    }
                }

                const int RESET_POINTER_COMMAND = 0x1;
                const int NUM_BYTES_PER_WORD = 4;
                int commandRegister = mAddr; // 0x43A0
                int dataRegister = mAddr + NUM_BYTES_PER_WORD; // 0x43A4


                // Send FPGA command to reset pointer 
                mDriver.ArrayWrite( commandRegister, new byte[] { RESET_POINTER_COMMAND } );

                // Loop to write the data to the buffer one 4 byte word at a time.
                int lengthInWords = length / NUM_BYTES_PER_WORD; // Todo core: Account for partial words??
                while( startIndex < lengthInWords )
                {
                    mDriver.ArrayWrite( dataRegister, data, startIndex, NUM_BYTES_PER_WORD );
                    startIndex += NUM_BYTES_PER_WORD;
                    if( !Poll( statusRegister ) )
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// If the module has an Eiger carrier then poll the status register bit field with a timeout
        /// Status Reg (0x4334) 
        /// BlazeBusy (Bit 1) == 1 => busy
        /// BlazeBusy (Bit 1) == 0 => okay to write next data set
        /// </summary>
        /// <param name="statusRegister"></param>
        /// <returns></returns>
        private static bool Poll( IRegister statusRegister )
        {
            DateTime startTime = DateTime.Now;
            const int TIME_OUT_MS = 10;
            const int BLAZE_BUSY = 0x1;
            while( ( statusRegister.Read32() & BLAZE_BUSY ) == BLAZE_BUSY )
            {
                TimeSpan timeSpan = DateTime.Now - startTime;
                if( timeSpan.TotalMilliseconds > TIME_OUT_MS )
                {
                    return false;
                }
            }
            return true;
        }

        private void LogBuffer8( string description, byte[] data, int count )
        {
            StringBuilder buffer = new StringBuilder( description );
            int n = Math.Min( count, 16 );
            for( int j = 0; j < n; j++ )
            {
                buffer.AppendFormat( "0x{0:x2},", data[ j ] );
            }
            if( n < count )
            {
                buffer.Append( "..." );
            }
            mLogger.LogAppend( new LoggingEvent( LogLevel.Finest, buffer.ToString() ) );
        }
    }
}