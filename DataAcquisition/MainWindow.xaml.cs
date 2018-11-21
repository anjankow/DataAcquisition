using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
using System.Diagnostics;

namespace DataAcquisition
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            var portChoiceWin = new PortChoiceWindow();
            portChoiceWin.ShowDialog();
            InitializeComponent();
            lbl_portName.Content = "PORT " + DataAcquisition.DataContext.Port;
            var serialPort = new SerialPort(DataAcquisition.DataContext.Port, 9600, Parity.None, 8, StopBits.One)
            {
                DtrEnable = true,
                Handshake = Handshake.XOnXOff
            };
            serialPort.Open();
            
            if(serialPort.IsOpen)
            {
                lbl_portName.Content="open";
               // connectionMarker.Fill = Resources["Connected"] as Brush;
            }
            else
            {
                lbl_portName.Content = "close";


                // connectionMarker.Fill = Application.GetResourceStream("Disconnected") as Brush;

            }
        }
        

        private void Btn_changePort_Click(object sender, RoutedEventArgs e)
        {
            var portChoiceWin = new PortChoiceWindow();
            portChoiceWin.ShowDialog();
            lbl_portName.Content = "PORT " + DataAcquisition.DataContext.Port;
        }

        private void Btn_configure_Click(object sender, RoutedEventArgs e)
        {
            var confWindow = new ConfWindow();
            confWindow.ShowDialog();
        }
    }
}
