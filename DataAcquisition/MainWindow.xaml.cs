using System;
using System.Windows;
using System.IO.Ports;
using Ookii.Dialogs.Wpf;
using System.Globalization;
using System.Collections.Generic;

namespace DataAcquisition
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public SerialPort serialPort;
        public Int16[] ADC1_rawData;
        public Int16[] ADC2_rawData;
        public Int16[] ADC3_rawData;
        public volatile bool isStopped;

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
            DataAcquisition.DataContext.MaxBufferSize = 16000;
            DataAcquisition.DataContext.SavePath=".";

            ADC1_rawData = new Int16[DataAcquisition.DataContext.MaxBufferSize];
            ADC2_rawData = new Int16[DataAcquisition.DataContext.MaxBufferSize];
            ADC3_rawData = new Int16[DataAcquisition.DataContext.MaxBufferSize];

            lbl_frequency.Content = DataAcquisition.DataContext.Frequency.ToString("0.###") + " Hz";
            lbl_mode.Content = DataAcquisition.DataContext.Mode == DataAcquisition.DataContext.Modes.SingleShot ?
                "single-shot" : "ciągły";
            btn_openDataFile.IsEnabled = false;
            progressBar.Value = 0;
        }

        private bool ReceiveDataBlock()
        {
            int headerNumberOfBytes = serialPort.ReadChar() - '0';
            string str_sizeOfDataInBytes = string.Empty;
            for (int i = 0; i < headerNumberOfBytes; i++)
            {
                str_sizeOfDataInBytes += serialPort.ReadChar();
            }
            int sizeOfDataInBytes = int.Parse(str_sizeOfDataInBytes);
            var dataInBytes = new Byte[sizeOfDataInBytes];
            try
            {
                serialPort.Read(dataInBytes, 0, sizeOfDataInBytes);
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message, "Exception!", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            
            for(int i = 0, j = 0; i < sizeOfDataInBytes / 2; i += 2, j++)
            {
                Int16 record;
                switch(DataAcquisition.DataContext.HowManyADC)
                {
                    case 1:
                        record = BitConverter.ToInt16(dataInBytes, i);
                        ADC1_rawData[j] = record;
                        break;
                    case 2:
                        record = BitConverter.ToInt16(dataInBytes, i);
                        ADC1_rawData[j] = record;
                        i += 2;
                        record = BitConverter.ToInt16(dataInBytes, i);
                        ADC2_rawData[j] = record;
                        break;
                    case 3:
                        record = BitConverter.ToInt16(dataInBytes, i);
                        ADC1_rawData[j] = record;
                        i += 2;
                        record = BitConverter.ToInt16(dataInBytes, i);
                        ADC2_rawData[j] = record;
                        i += 2;
                        record = BitConverter.ToInt16(dataInBytes, i);
                        ADC3_rawData[j] = record;
                        break;
                    default:
                        throw new ArgumentException();
                }
            }
            return true;
        }


        private void Receive(object sender, SerialDataReceivedEventArgs e)
        {
            char data = (char)serialPort.ReadChar();
            MessageBox.Show(": " + data);
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
            if (String.Equals(DataAcquisition.DataContext.Port, previousPort))
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
            if (serialPort.IsOpen)
            {
                CreateDataFiles(out string [] fileNames);
                serialPort.WriteLine("ACQuire:SRATe " + DataAcquisition.DataContext.Frequency.ToString("0.###"));
                serialPort.WriteLine("ACQuire:POINts " + DataAcquisition.DataContext.BufferSize.ToString());
                for (int i = 1; i <= DataAcquisition.DataContext.HowManyADC; i++)
                {
                    serialPort.WriteLine("ROUT:ENAB 1,(@10" + i + ")");
                }

                if (DataAcquisition.DataContext.Mode == DataAcquisition.DataContext.Modes.Continuous)
                {
                    serialPort.WriteLine("RUN");
                }
                else
                {
                    serialPort.WriteLine("DIG");
                }

                ReceiveDataLoop(fileNames);
            }
        }

        private void CreateDataFiles(out string[] fileName)
        {
            string dateTimeNow = DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_" + String.Format("{0:D2}", DateTime.Now.Hour) + " -" + String.Format("{0:D2}", DateTime.Now.Minute);
            fileName = new string[3];
            fileName[0] = DataAcquisition.DataContext.SavePath + "\\ADC1_pomiary_" + dateTimeNow + ".csv";
            fileName[1] = DataAcquisition.DataContext.SavePath + "\\ADC2_pomiary_" + dateTimeNow + ".csv";
            fileName[2] = DataAcquisition.DataContext.SavePath + "\\ADC3_pomiary_" + dateTimeNow + ".csv";

            switch (DataAcquisition.DataContext.HowManyADC)
            {
                case 3:
                    using (var streamWriter = new System.IO.StreamWriter(fileName[2], false))
                    {
                        streamWriter.Write("frequency," + DataAcquisition.DataContext.Frequency.ToString() + ",Hz\n\n");
                        var csvWriter = new CsvHelper.CsvWriter(streamWriter);
                    }
                    goto case 2;
                case 2:
                    using (var streamWriter = new System.IO.StreamWriter(fileName[1], false))
                    {
                        streamWriter.Write("frequency," + DataAcquisition.DataContext.Frequency.ToString() + ",Hz\n\n");
                        var csvWriter = new CsvHelper.CsvWriter(streamWriter);
                    }
                    goto case 1;
                case 1:
                    using (var streamWriter = new System.IO.StreamWriter(fileName[0], false))
                    {
                        streamWriter.Write("frequency," + DataAcquisition.DataContext.Frequency.ToString() + ",Hz\n\n");
                        var csvWriter = new CsvHelper.CsvWriter(streamWriter);
                    }
                    break;
                default:
                    throw new ArgumentException();
            
            }
            
            MessageBox.Show("Przygotowano pliki", "Pliki przygotowane", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        

        private void ReceiveDataLoop(string[] fileNames)
        {
            serialPort.ReadTimeout = 2000;
            while (true)
            {
                serialPort.WriteLine("WAVeform:COMP?");
                string isReady;
                try
                {
                    isReady = serialPort.ReadLine();
                    if (isReady == "YES")
                    {
                        serialPort.WriteLine("WAVeform:DATA?");
                    }
                    if(ReceiveDataBlock())
                    {
                        SaveToCSV(fileNames);
                    }
                    if (isStopped || DataAcquisition.DataContext.Mode == DataAcquisition.DataContext.Modes.SingleShot)
                    {
                        //if (a user pressed "STOP" button  and mode is continuous) or (mode is single shot) -> break while loop
                        break;
                    }
                }
                catch (InvalidOperationException ex1)
                {
                    MessageBox.Show(ex1.Message, "Invalid Operation Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                }
                catch (TimeoutException ex2)
                {
                    MessageBox.Show(ex2.Message, "Timeout Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }

            }
        }

        private void SaveToCSV(string[] fileNames)
        {
            
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
        
        private void Btn_openCSVfile(object sender, RoutedEventArgs e)
        {

        }

        private void Btn_saveDestination_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog();
            VistaFolderBrowserDialog dlg = new VistaFolderBrowserDialog();
            dlg.ShowNewFolderButton = true;
            if (dlg.ShowDialog()==true)
            {
                DataAcquisition.DataContext.SavePath = dlg.SelectedPath;
            }
        }
    }
    
}