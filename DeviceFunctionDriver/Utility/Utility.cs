using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceFunctionDriver
{
    internal static class Utility
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

            double ifBandwidth = 10e6;
            const double NOISE_VOLT_DENSITY = 2.0e-8;
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
                ampdata[i] = 20 * (double)Math.Log10(Math.Sqrt(idata[i] * idata[i] + qdata[i] * qdata[i]));
            }

            var temp = ampdata[fftsize / 2];
            ampdata[fftsize / 2] = ampdata[0];
            ampdata[0] = temp;

            return ampdata;
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

    }
}
