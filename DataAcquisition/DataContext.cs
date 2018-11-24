using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAcquisition
{
    public static class DataContext
    {
        public static string Port { get; set; }
        public static int BufferSize { get; set; }
        public static int HowManyBuffers { get; set; }
        public static int Frequency { get; set; }
        public static int HowManyADC { get; set; }

    }
}
