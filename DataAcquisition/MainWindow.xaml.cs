using System;
using System.Windows;
using System.IO.Ports;
using Ookii.Dialogs.Wpf;
using System.Globalization;

namespace DataAcquisition
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public SerialPort serialPort;
        public Int16[] ADC1_rawData= { 56, 9865, 4500 };
        public Int16[] ADC2_rawData = { 656, 55, 5, 4, 1 };
        public Int16[] ADC3_rawData = { 0 };

        public MainWindow()
        {
            InitializeComponent();

            //if(!ChooseAndOpenPort())
            //{
            //    //port choice window closed, end of program
            //    this.Close();
            //}

            DataAcquisition.DataContext.Port = String.Empty;
            DataAcquisition.DataContext.Mode = DataAcquisition.DataContext.Modes.SingleShot;
            DataAcquisition.DataContext.HowManyADC = 1;
            DataAcquisition.DataContext.Frequency = 1000;
            DataAcquisition.DataContext.BufferSize = 1600;

            lbl_frequency.Content = DataAcquisition.DataContext.Frequency.ToString("0.###")+" Hz";
            lbl_mode.Content = DataAcquisition.DataContext.Mode == DataAcquisition.DataContext.Modes.SingleShot ?
                "single-shot" : "ciągły";
            btn_saveToCsv.IsEnabled = false;
            progressBar.Value = 0;
        }

        private void ReceiveMesaurementsData(Int16[] dataDestination)
        {
            //dataDestination is pointed to exact location in a huge array for all records
            //that way offset doesn't have to be specified
            int headerNumberOfBytes = serialPort.ReadChar()-'0';
            string str_sizeOfDataInBytes = string.Empty;
            for(int i=0;i<headerNumberOfBytes;i++)
            {
                str_sizeOfDataInBytes += serialPort.ReadChar();
            }
            int sizeOfDataInBytes = int.Parse(str_sizeOfDataInBytes);
            var dataInBytes = new Byte[sizeOfDataInBytes];
            serialPort.Read(dataInBytes, 0, sizeOfDataInBytes);
            for (int i=0,j=0;i<sizeOfDataInBytes/2;i+=2,j++)
            {
                Int16 record = BitConverter.ToInt16(dataInBytes, i);
                dataDestination[j] = record;
            }
            
            
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
            lbl_frequency.Content = DataAcquisition.DataContext.Frequency.ToString("0.###") + " Hz";
            lbl_mode.Content = DataAcquisition.DataContext.Mode == DataAcquisition.DataContext.Modes.SingleShot ?
                "single-shot" : "ciągły";
        }

        private void Btn_start_Click(object sender, RoutedEventArgs e)
        {
            if(serialPort.IsOpen)
            {
                serialPort.WriteLine("ACQuire :SRATe " + DataAcquisition.DataContext.Frequency.ToString("0.###E+0", CultureInfo.InvariantCulture));
                serialPort.WriteLine("ACQuire :POINts " + DataAcquisition.DataContext.BufferSize.ToString());
                serialPort.WriteLine("WAVeform :DATA?");
                serialPort.ReadTimeout = 2000;

                try
                {
                    string odp = serialPort.ReadLine();
                    MessageBox.Show(odp);
                }
                catch(TimeoutException)
                {
                    MessageBox.Show("timeout");
                }
            }
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

        private void Btn_saveToCsv_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog();
            VistaFolderBrowserDialog dlg = new VistaFolderBrowserDialog();
            dlg.ShowNewFolderButton = true;
            if(dlg.ShowDialog() != true)
            {
                return;
            }
            string dateTimeNow = DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_" + String.Format("{0:D2}",DateTime.Now.Hour) + " -" + String.Format("{0:D2}", DateTime.Now.Minute);
            string fileName1 = dlg.SelectedPath + "\\ADC1_pomiary_" + dateTimeNow  + ".csv";
            string fileName2 = dlg.SelectedPath + "\\ADC2_pomiary_" + dateTimeNow + ".csv";
            string fileName3 = dlg.SelectedPath + "\\ADC3_pomiary_" + dateTimeNow + ".csv";

            using (var streamWriter = new System.IO.StreamWriter(fileName1,false))
            {
                streamWriter.Write("frequency," + DataAcquisition.DataContext.Frequency.ToString() + ",Hz\n\n");
                var csvWriter = new CsvHelper.CsvWriter(streamWriter);
                csvWriter.WriteRecords(ADC1_rawData);
            }
            using (var streamWriter = new System.IO.StreamWriter(fileName2, false))
            {
                streamWriter.Write("frequency," + DataAcquisition.DataContext.Frequency.ToString() + ",Hz\n\n");
                var csvWriter = new CsvHelper.CsvWriter(streamWriter);
                csvWriter.WriteRecords(ADC2_rawData);
            }
            using (var streamWriter = new System.IO.StreamWriter(fileName3, false))
            {
                streamWriter.Write("frequency," + DataAcquisition.DataContext.Frequency.ToString() + ",Hz\n\n");
                var csvWriter = new CsvHelper.CsvWriter(streamWriter);
                csvWriter.WriteRecords(ADC3_rawData);
            }
            MessageBox.Show("Zapisano wyniki we wskazanej lokalizacji", "Wyniki zapisane", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}