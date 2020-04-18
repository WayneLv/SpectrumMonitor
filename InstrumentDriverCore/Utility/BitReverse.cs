/******************************************************************************
 *                                                                         
 *               Copyright 2011 .
 *               All rights reserved.
 *
 *****************************************************************************/
using System;

namespace InstrumentDriver.Core.Utility
{
    public static class BitReverse
    {
        public static Int16 ReverseBits( Int16 bitsToSwap )
        {
            Int16 swappedBits = 0;
            Int32 mask = 0x8000;

            for( int i = 0; i < 16; i++, bitsToSwap >>= 1, mask >>= 1 )
            {
                if( ( bitsToSwap & 0x1 ) == 1 )
                {
                    swappedBits |= (Int16)mask;
                }
            }

            return swappedBits;
        }
    }
}