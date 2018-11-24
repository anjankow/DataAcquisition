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
        public SerialPort serialPort;
        char[] ADC1_data; 
        public MainWindow()
        {
            InitializeComponent();
            DataAcquisition.DataContext.Port = String.Empty;
            DataAcquisition.DataContext.HowManyBuffers = 100;
            DataAcquisition.DataContext.HowManyADC = 1;
            DataAcquisition.DataContext.Frequency = 1000;
            DataAcquisition.DataContext.BufferSize = 1600;

            /*if(!ChooseAndOpenPort())
            {
                //port choice window closed, end of program
                this.Close();
            }*/
            
            
        }

        private void ReceiveMesaurementsData(object sender, SerialDataReceivedEventArgs e)
        {
            
        }

        private void Receive(object sender, SerialDataReceivedEventArgs e)
        {
            char data=(char)serialPort.ReadChar();
            MessageBox.Show(": "+data);
        }

        private void ErrorHandler(object sender, SerialErrorReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private bool ChooseAndOpenPort()
        {
            string previousPort = DataAcquisition.DataContext.Port;
            var portChoiceWin = new PortChoiceWindow();
            bool isPortChosen = (bool)portChoiceWin.ShowDialog();
            
            if (!isPortChosen)
            {
                return false;
            }
            if (String.Equals(DataAcquisition.DataContext.Port,previousPort))
            {
                return true;
            }
            lbl_portName.Content = "PORT " + DataAcquisition.DataContext.Port;
            serialPort = new SerialPort(DataAcquisition.DataContext.Port, 9600, Parity.None, 8, StopBits.One);
            serialPort.Open();
            if (!serialPort.IsOpen)
            {
                MessageBox.Show("Port nie został otworzony! Wybierz port jeszcze raz", "Błąd portu",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                ChooseAndOpenPort();
            }
            serialPort.DataReceived += new SerialDataReceivedEventHandler(Receive);
            serialPort.ErrorReceived += new SerialErrorReceivedEventHandler(ErrorHandler);
            return true;
        }

        private void Btn_changePort_Click(object sender, RoutedEventArgs e)
        {
            ChooseAndOpenPort();
        }

        private void Btn_configure_Click(object sender, RoutedEventArgs e)
        {
            var confWindow = new ConfWindow();
            confWindow.ShowDialog();
        }

        private void Btn_start_Click(object sender, RoutedEventArgs e)
        {
            ReactIfPortNotOpen();
            serialPort.Write("o");  //start programu w STM
            serialPort.Write("a");
        }


        private void ReactIfPortNotOpen()
        {
            if (!serialPort.IsOpen)
            {
                MessageBox.Show("Port nie został otworzony! Wybierz port jeszcze raz", "Błąd portu",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                ChooseAndOpenPort();
            }
        }
        
        
    }
}

//connectionMarker.Visibility = Visibility.Visible;
//btn_configure.IsEnabled = true;
//btn_start.IsEnabled = true;