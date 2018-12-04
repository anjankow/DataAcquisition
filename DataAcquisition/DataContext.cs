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
    }
}
