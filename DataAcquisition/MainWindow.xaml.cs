using System;
using System.Windows;
using System.IO.Ports;
using Ookii.Dialogs.Wpf;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows.Media;

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

        public int readTimeout = 2000;
        public string[] fileName;


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
            btn_showFiles.IsEnabled = false;
            progressBar.IsIndeterminate = false;
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
                throw new Exception("exception in ReceiveData function: " + e.Message);
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
                        break;
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



        private void StartMeasurements(object sender, RoutedEventArgs e)
        {
            if (serialPort.IsOpen)
            {
                isStopped = false;
                ChangeToStop();
                CreateDataFiles();
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

                Thread receiveDataThread = new Thread(ReceiveDataLoop);
                receiveDataThread.Start();
            }
            else
            {
                MessageBox.Show("Port nie jest otwarty! Wybierz port jeszcze raz", "Port nieotwarty", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }

        }

        private void StopMeasurements(object sender, RoutedEventArgs e)
        {
            isStopped = true;
            ChangeToStart();
        }

        private void ChangeToStop()
        {
            btn_start.Content = "STOP";
            btn_start.Background = Resources["BtnStop"] as SolidColorBrush;
            btn_start.Click -= StartMeasurements;
            btn_start.Click += StopMeasurements;
        } 

        private void ChangeToStart()
        {
            btn_start.Content = "START";
            btn_start.Background = Resources["BtnStart"] as SolidColorBrush;
            btn_start.Click += StartMeasurements;
            btn_start.Click -= StopMeasurements;
        }

        private void CreateDataFiles()
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
        

        private void ReceiveDataLoop()
        {
            progressBar.IsIndeterminate = true;
            serialPort.ReadTimeout = readTimeout;
            Thread saveToFileThread = new Thread(SaveToCSV);
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
                        if (ReceiveDataBlock())
                        {
                            if(saveToFileThread.ThreadState==ThreadState.Running)
                            {
                                throw new Exception("Saving previous data to a file is not finished");
                            }
                            else
                            {
                                saveToFileThread.Start();
                            }
                        }
                        if (DataAcquisition.DataContext.Mode == DataAcquisition.DataContext.Modes.SingleShot)
                        {
                            break;
                        }
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
                catch(Exception ex3)
                {
                    MessageBox.Show(ex3.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                if (isStopped)
                {
                    //if a user pressed "STOP" button
                    break;
                }

            }
            progressBar.IsIndeterminate = false;
        }

        private void SaveToCSV()
        {
            switch (DataAcquisition.DataContext.HowManyADC)
            {
                case 3:
                    using (var streamWriter = new System.IO.StreamWriter(fileName[2], true))
                    {
                        var csvWriter = new CsvHelper.CsvWriter(streamWriter);
                        csvWriter.WriteRecords(ADC3_rawData);
                    }
                    goto case 2;
                case 2:
                    using (var streamWriter = new System.IO.StreamWriter(fileName[1], true))
                    {
                        var csvWriter = new CsvHelper.CsvWriter(streamWriter);
                        csvWriter.WriteRecords(ADC2_rawData);
                    }
                    goto case 1;
                case 1:
                    using (var streamWriter = new System.IO.StreamWriter(fileName[0], true))
                    {
                        var csvWriter = new CsvHelper.CsvWriter(streamWriter);
                        csvWriter.WriteRecords(ADC1_rawData);
                    }
                    break;
                default:
                    break;

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
        

        private void Btn_saveDestination_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog();
            VistaFolderBrowserDialog dlg = new VistaFolderBrowserDialog
            {
                ShowNewFolderButton = true,
                SelectedPath = DataAcquisition.DataContext.SavePath
            };
            if (dlg.ShowDialog()==true)
            {
                DataAcquisition.DataContext.SavePath = dlg.SelectedPath;
            }
        }

        private void Btn_showFiles_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.FileName = "explorer";
            process.StartInfo.Arguments = DataAcquisition.DataContext.SavePath;
            process.Start();
        }
    }
    
}