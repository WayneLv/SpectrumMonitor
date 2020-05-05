using InstrumentDriverCore.Interfaces;
using InstrumentDriverCore.Mock;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstrumentDriver.Core.Utility
{
    public static class Utility
    {

        static Random rd = new Random();
        public static double[] SimulateSpectrumData(int samples)
        {
            double[] idata = new double[samples];
            double[] qdata = new double[samples];
            

            const double MINUS_TEN_DBM = 0.0707107;

            // Set initial I/Q values to near zero, flat signal level
            double iVal = 1.0e-8;
            double qVal = 1.0e-8;

            // Simulate signal depending on simulation type

            iVal = MINUS_TEN_DBM;
            qVal = iVal;

            int numberOfPoints = samples;

            double ifBandwidth = 1e6;
            const double NOISE_VOLT_DENSITY = 2.0e-6;
            double noiseVoltage = (double)Math.Sqrt(ifBandwidth) * NOISE_VOLT_DENSITY;

            // Copy simulated data to destination buffer
            for (int i = 0; i < numberOfPoints; i++)
            {
                idata[i] = (double)iVal + GenerateRandomNoise(noiseVoltage);
                qdata[i] = (double)qVal + GenerateRandomNoise(noiseVoltage);
            }

            int fftsize = FFT(idata, qdata);

            double[] ampdata = new double[fftsize];
            for (int i = 0; i < fftsize; i++)
            {
                ampdata[i] = 20 * (double)Math.Log10(Math.Sqrt(idata[i] * idata[i] + qdata[i] * qdata[i])) - 50;
            }

            var temp = ampdata[fftsize / 2];
            ampdata[fftsize / 2] = ampdata[0];
            ampdata[0] = temp;

            return ampdata;
        }

        public static List<ISignalCharacters> SimulateSignalCharacters()
        {
            List<ISignalCharacters> signallist = new List<ISignalCharacters>();


            Random rd = new Random();
            int signalcnt = rd.Next(5, 10);
            for (int i = 0; i < signalcnt; i++)
            {
                MockSignalCharacters mocksig = new MockSignalCharacters();
                signallist.Add(mocksig);
            }

            return signallist;
        }

        private static double GenerateRandomNoise(double noiseVoltage)
        {

            
            double rnd = rd.NextDouble();

            // Unity noise is a random number between -1.0 and 1.0
            double unityNoise = 2.0 * (rnd - 0.5);
            double sign = 0.0;

            if (unityNoise < 0)
            {
                sign = -1;
                unityNoise = Math.Abs(unityNoise);
            }
            else
            {
                sign = 1;
            }

            // Make noise value unity in the power equation.
            double noise = 0.7071 * noiseVoltage * unityNoise * sign;

            return (double)noise;
        }
        private static void bitrp(double[] xreal, double[] ximag, int n)
        {
            // 位反转置换 Bit-reversal Permutation
            int i, j, a, b, p;
            for (i = 1, p = 0; i < n; i *= 2)
            {
                p++;
            }
            for (i = 0; i < n; i++)
            {
                a = i;
                b = 0;
                for (j = 0; j < p; j++)
                {
                    b = b * 2 + a % 2;
                    a = a / 2;
                }
                if (b > i)
                {
                    double t = xreal[i];
                    xreal[i] = xreal[b];
                    xreal[b] = t;
                    t = ximag[i];
                    ximag[i] = ximag[b];
                    ximag[b] = t;
                }
            }
        }

        public static int FFT(double[] xreal, double[] ximag)
        {
            int n = 2;
            while (n <= xreal.Length)
            {
                n *= 2;
            }
            n /= 2;
            double[] wreal = new double[n / 2];
            double[] wimag = new double[n / 2];
            double treal, timag, ureal, uimag, arg;
            int m, k, j, t, index1, index2;
            bitrp(xreal, ximag, n);
            arg = (double)(-2 * Math.PI / n);
            treal = (double)Math.Cos(arg);
            timag = (double)Math.Sin(arg);
            wreal[0] = 1.0;
            wimag[0] = 0.0;
            for (j = 1; j < n / 2; j++)
            {
                wreal[j] = wreal[j - 1] * treal - wimag[j - 1] * timag;
                wimag[j] = wreal[j - 1] * timag + wimag[j - 1] * treal;
            }
            for (m = 2; m <= n; m *= 2)
            {
                for (k = 0; k < n; k += m)
                {
                    for (j = 0; j < m / 2; j++)
                    {
                        index1 = k + j;
                        index2 = index1 + m / 2;
                        t = n * j / m; 
                        treal = wreal[t] * xreal[index2] - wimag[t] * ximag[index2];
                        timag = wreal[t] * ximag[index2] + wimag[t] * xreal[index2];
                        ureal = xreal[index1];
                        uimag = ximag[index1];
                        xreal[index1] = ureal + treal;
                        ximag[index1] = uimag + timag;
                        xreal[index2] = ureal - treal;
                        ximag[index2] = uimag - timag;
                    }
                }
            }
            return n;
        }


        static double k = 1e3;
        static double M = 1e6;
        static double G = 1e9;
        static string Hz = "Hz";
        static string kHz = "kHz";
        static string MHz = "MHz";
        static string GHz = "GHz";
        public static string FrequencyValueToString(double value)
        {
            string HzFormat = "{0:F0} {1}"; ;
            string kHzFormat = "{0:0.###} {1}";
            string MHzFormat = "{0:0.######} {1}";
            string GHzFormat = "{0:0.#########} {1}";

            double absvalue = Math.Abs(value);
            double unitVal = value;
            string unit = Hz;
            string format = HzFormat;


            if (absvalue >= G)
            {
                unitVal = value / G;
                unit = GHz;
                format = GHzFormat;
            }
            else if (absvalue >= M)
            {
                unitVal = value / M;
                unit = MHz;
                format = MHzFormat;
            }
            else if (absvalue >= k)
            {
                unitVal = value / k;
                unit = kHz;
                format = kHzFormat;
            }

            return String.Format(format, unitVal, unit);

        }

        public static double FrequencyStringToValue(string valStr)
        {
            string[] vals = valStr.Split(' ');
            if (vals.Length != 2)
                return 0.0;

            double value = 0.0;
            double scale = 1;

            if (!double.TryParse(vals[0], out value))
            {
                return value;
            }

            if (vals[1].ToUpper() == GHz.ToUpper())
            {
                scale = G;
            }
            else if (vals[1].ToUpper() == MHz.ToUpper())
            {
                scale = M;
            }
            else if (vals[1].ToUpper() == kHz.ToUpper())
            {
                scale = k;
            }

            return scale * value;

        }

        public static double[] WaveformInterpolation(double[] sourceWaveform, int targetPoints)
        {
            if (sourceWaveform == null)
                return null;
            if (sourceWaveform.Length == targetPoints)
            {
                return sourceWaveform;
            }

            if (targetPoints < 10 || targetPoints > 100000)
            {
                throw new Exception(String.Format("Target points({0}) is too small or larger", targetPoints));
            }
            double[] newWaveform = new double[targetPoints];
            double indexScale = (double)(sourceWaveform.Length)/ (double)targetPoints;
            for (int i = 0; i < targetPoints; i++)
            {
                int index = (int)(i * indexScale);
                newWaveform[i] = sourceWaveform[index];
            }
            return newWaveform;
        }

        public static double ClipForRBWSetting(double value)
        {
            double[] validstep = { 1, 2, 5 ,10};
            double clipvalue = value;

            while (clipvalue > 10.0)
            {
                clipvalue /= 10;
            }
            double power = value/ clipvalue;

            for (int i = 0; i < validstep.Length -1; i++)
            {
                if (clipvalue > validstep[i] && clipvalue <= validstep[i+1])
                {
                    if (clipvalue > (validstep[i] + validstep[i + 1]) / 2)
                    {
                        clipvalue = validstep[i + 1];
                    }
                    else
                    {
                        clipvalue = validstep[i];
                    }
                    break;
                }
            }

            return clipvalue * power;
        }


        public static int ParseInt(string numberStr)
        {
            int parsedInt = 0;

            bool status = ConvertStringInt(numberStr, ref parsedInt);
            if (status == false)
            {
                throw new Exception("Invalid address or data.  Enter integer value in decimal <nnn> or hex <0xnnn>");
            }
            return parsedInt;
        }

        public static bool ConvertStringInt(string numberStr, ref int numberValue)
        {
            bool status = true;
            {
                try
                {
                    numberStr = numberStr.Trim().ToUpperInvariant();
                    if (numberStr.Length > 0)
                    {
                        // Try to parse as decimal
                        if (numberStr.StartsWith("0X"))
                        {
                            numberStr = numberStr.Substring(2);
                            // Parse as hex string
                            numberValue = Int32.Parse(numberStr, NumberStyles.HexNumber);
                        }
                        else
                        {
                            // Parse as decimal string
                            // This may throw an exception, if not a value number.
                            // Will be caught as displayed as user error
                            numberValue = Int32.Parse(numberStr);
                        }
                    }
                }
                catch
                {
                    status = false;
                }
            }
            return status;
        }

        public static bool ConvertStringLong(string numberStr, ref long numberValue)
        {
            bool status = true;
            {
                try
                {
                    numberStr = numberStr.Trim().ToUpperInvariant();
                    if (numberStr.Length > 0)
                    {
                        // Try to parse as decimal
                        if (numberStr.StartsWith("0X"))
                        {
                            numberStr = numberStr.Substring(2);
                            // Parse as hex string
                            numberValue = Int64.Parse(numberStr, NumberStyles.HexNumber);
                        }
                        else
                        {
                            // Parse as decimal string
                            // This may throw an exception, if not a value number.
                            // Will be caught as displayed as user error
                            numberValue = Int64.Parse(numberStr);
                        }
                    }
                }
                catch
                {
                    status = false;
                }
            }
            return status;
        }

    }
}
