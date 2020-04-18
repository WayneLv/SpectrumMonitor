/******************************************************************************
 *                                                                         
 *               Copyright 2012 .
 *               All rights reserved.
 *
 *****************************************************************************/
using System;
using System.Runtime.InteropServices;
using System.Reflection.Emit;

namespace InstrumentDriver.Core.Utility
{
    // ReSharper disable InconsistentNaming
    [ComVisible( false )]
    public enum Units
    {
        [Description( "" )] Unitless,
        [Description( "us" )] Microseconds,
        [Description( "ns" )] Nanoseconds,
        [Description( "ms" )] Milliseconds,
        [Description( "sec" )] Seconds,
        [Description( "Hz" )] Hz,
        [Description( "kHz" )] KHz,
        [Description( "MHz" )] MHz,
        [Description( "GHz" )] GHz,
        [Description( "dBm" )] dBm,
        [Description( "dB" )] dB,
        [Description( "mW" )] mW,
        [Description( "W" )] W,
        [Description( "VRMS" )] vRMS,
        [Description( "V" )] Volts,
        [Description( "mV" )] mV,
        [Description( "A" )] Amps,
        [Description( "mA" )] mA,
        [Description( "dBm/V" )] dBm_per_volt,
        [Description( "Hz/V" )] Hz_per_volt,
        [Description( "Deg" )] Degrees,
        [Description( "%" )] Percentage,
    }

    // ReSharper restore InconsistentNaming

    public class UnitUtil
    {
        /// <summary>
        /// Convert a value between compatible units.
        /// </summary>
        /// <param name="inUnits">The units of the input value</param>
        /// <param name="outUnits">The units of the output value</param>
        /// <param name="inValue">The value to convert</param>
        /// <returns>The specified value converted to the specified units</returns>
        public static double Convert(double inValue, Units inUnits, Units outUnits)
        {
            // REVISIT (bwatkins)
            // This method is incomplete, and needs to be greatly expanded.

            // Convert between peak Volts and dBm.
            // The equations are simplified knowing that
            // RMS Volts = (peak Volts / Sqrt(2))
            // 20 * Log10(1 / Sqrt(2)) = -3.01
            if ((inUnits == Units.Volts) && (outUnits == Units.dBm))
            {
                return (20 * Math.Log10(inValue) + 10);
            }
            if ((inUnits == Units.dBm) && (outUnits == Units.Volts))
            {
                return Math.Pow(10, ((inValue - 10) / 20));
            }

            // Convert between RMS Volts and dBm.
            // dBV = 20 * Log10(RMS Volts)
            if ((inUnits == Units.vRMS) && (outUnits == Units.dBm))
            {
                return (20 * Math.Log10(inValue) + 13.01);
            }
            if ((inUnits == Units.dBm) && (outUnits == Units.vRMS))
            {
                return Math.Pow(10, ((inValue - 13.01) / 20));
            }

            throw new Exception("Unsupported unit conversion");
        }

        /// <summary>
        /// Return a dynamically generated delegate method to convert an arbitrary
        /// enum to an arbitrary value type
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public static Func<TEnum, TResult> CreateFromEnumConverter<TEnum, TResult>()
            where TEnum : struct
            where TResult : struct
        {
            Type underlyingType = Enum.GetUnderlyingType(typeof (TEnum));

            // Create a dynamic method and gets its Intermediate Language (IL) generator.
            var dynam = new DynamicMethod("__" + typeof (TEnum).Name + "_to_" + typeof (TResult).Name,
                                          typeof (TResult), new[] {typeof (TEnum)}, true);
            ILGenerator il = dynam.GetILGenerator();

            // Emit IL code to return the enum value as the return value.
            // If the size of the enum type and the value type differ, an
            // attempt is made to convert the value size.
            il.Emit(OpCodes.Ldarg_0, 0);
            int resultSize = Marshal.SizeOf(typeof (TResult));
            if (resultSize != Marshal.SizeOf(underlyingType))
            {
                EmitConversionOpcode(il, resultSize);
            }
            il.Emit(OpCodes.Ret);

            // Return a delegate for the dynamic method.
            return (Func<TEnum, TResult>) dynam.CreateDelegate(typeof (Func<TEnum, TResult>));
        }

        /// <summary>
        /// Return a dynamically generated delegate method to convert an arbitrary
        /// value type to an arbitrary enum
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TEnum"></typeparam>
        /// <returns></returns>
        public static Func<TInput, TEnum> CreateToEnumConverter<TInput, TEnum>()
            where TEnum : struct
            where TInput : struct
        {
            Type underlyingType = Enum.GetUnderlyingType(typeof (TEnum));

            // Create a dynamic method and gets its Intermediate Language (IL) generator.
            var dynam = new DynamicMethod("__" + typeof(TInput).Name + "_to_" + typeof(TEnum).Name,
                                          typeof(TEnum), new[] {typeof (TInput)}, true);
            ILGenerator il = dynam.GetILGenerator();

            // Emit IL code to return the value type as the enum.
            // If the size of the enum type and the value type differ, an
            // attempt is made to convert the value size.
            il.Emit(OpCodes.Ldarg_0, 0);
            int enumSize = Marshal.SizeOf(underlyingType);
            if (enumSize != Marshal.SizeOf(typeof(TInput)))
            {
                EmitConversionOpcode(il, enumSize);
            }
            il.Emit(OpCodes.Ret);

            // Return a delegate for the dynamic method.
            return (Func<TInput, TEnum>)dynam.CreateDelegate(typeof(Func<TInput, TEnum>));
        }

        /// <summary>
        /// Emit IL code to convert the stack value to the specified size
        /// </summary>
        /// <param name="il"></param>
        /// <param name="resultSize"></param>
        private static void EmitConversionOpcode(ILGenerator il, int resultSize)
        {
            if (resultSize <= 0)
            {
                throw new ArgumentOutOfRangeException("resultSize", resultSize, "Result size must be a power of 2");
            }

            int n = 0;
            
            while (true)
            {
                if (n >= _converterOpCodes.Length)
                {
                    throw new ArgumentOutOfRangeException("resultSize", resultSize, "Invalid result size");
                }

                if ((resultSize >> n) == 1)
                {
                    il.Emit(_converterOpCodes[n]);
                    return;
                }
                
                n++;
            }
        }

        // IL opcodes for integer value conversion.
        private static readonly OpCode[] _converterOpCodes = new[]
            { OpCodes.Conv_I1, OpCodes.Conv_I2, OpCodes.Conv_I4, OpCodes.Conv_I8 };
    }
}