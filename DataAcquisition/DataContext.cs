using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAcquisition
{
    public static class DataContext
    {
        public enum Modes {SingleShot, Continuous };
        public static string Port { get; set; }
        public static int BufferSize { get; set; }
        public static double Frequency { get; set; }
        public static int HowManyADC { get; set; }
        public static Modes Mode { get; set; }
        public static string SavePath { get; set; }

        public const int MaxBufferSize = 16000;
        public const int MinBufferSize = 1;
        public const double MaxFrequency = 1000000;
        public const double MinFrequency = 0.5;
    }
}
