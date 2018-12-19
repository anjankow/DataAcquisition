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
using System.Diagnostics;
using System.Linq;

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

        Thread receiveDataThread;

        public event EventHandler MeasurementsStart;
        public event EventHandler MeasurementsStop;

        public MainWindow()
        {
            InitializeComponent();

            if (!ChooseAndOpenPort())
            {
                //port choice window closed, end of program
                Close();
            }

            DataAcquisition.DataContext.Port = string.Empty;
            DataAcquisition.DataContext.Mode = DataAcquisition.DataContext.Modes.SingleShot;
            DataAcquisition.DataContext.HowManyADC = 1;
            DataAcquisition.DataContext.Frequency = 1000;
            DataAcquisition.DataContext.BufferSize = 3000;
            DataAcquisition.DataContext.SavePath = DataAcquisition.DataContext.DefaultPath;

            lbl_frequency.Content = DataAcquisition.DataContext.Frequency.ToString("0.###") + " Hz";
            lbl_mode.Content = DataAcquisition.DataContext.Mode == DataAcquisition.DataContext.Modes.SingleShot ?
                "single-shot" : "ciągły";
            btn_showFiles.IsEnabled = false;
            progressBar.IsIndeterminate = false;

            MeasurementsStart += OnMeasurementsStart;
            MeasurementsStop += OnMeasurementsStop;

            receiveDataThread = new Thread(ReceiveDataLoop);
        }

        protected void OnMeasurementsStart(object sender, EventArgs e)
        {
            btn_start.Content = "STOP";
            btn_start.Background = Resources["BtnStop"] as SolidColorBrush;
            btn_start.Click -= StartMeasurements;
            btn_start.Click += StopMeasurements;
            btn_showFiles.IsEnabled = false;
            btn_saveDestination.IsEnabled = false;
            btn_configure.IsEnabled = false;
            btn_changePort.IsEnabled = false;
            progressBar.IsIndeterminate = true;
        }

        protected void OnMeasurementsStop(object sender, EventArgs e)
        {
            btn_start.Dispatcher.Invoke(() => {
                btn_start.Content = "START";
                btn_start.Background = Resources["BtnStart"] as SolidColorBrush;
                btn_start.Click += StartMeasurements;
                btn_start.Click -= StopMeasurements;
            });

            btn_showFiles.Dispatcher.Invoke(() => { btn_showFiles.IsEnabled = true; });
            btn_saveDestination.Dispatcher.Invoke(() => { btn_saveDestination.IsEnabled = true; });
            btn_configure.Dispatcher.Invoke(() => { btn_configure.IsEnabled = true; });
            btn_showFiles.Dispatcher.Invoke(() => { btn_changePort.IsEnabled = true; });
            progressBar.Dispatcher.Invoke(() => { progressBar.IsIndeterminate = false; });

        }

        private bool ReceiveDataBlock()
        {
            while (serialPort.ReadChar() != '#') ;
            int headerNumberOfBytes = serialPort.ReadChar() - '0';
            string str_sizeOfDataInBytes = string.Empty;
            for (int nr = 0; nr < headerNumberOfBytes; nr++)
            {
                str_sizeOfDataInBytes += serialPort.ReadChar() - '0';
            }
            int sizeOfDataInBytes = int.Parse(str_sizeOfDataInBytes);
            var dataInBytes = new Byte[sizeOfDataInBytes];
            try
            {
                serialPort.Read(dataInBytes, 0, sizeOfDataInBytes);
            }
            catch(Exception e)
            {
                throw new Exception("Exception in ReceiveData function: " + e.Message);
            }

            Int16 record;
            int i, j;
            int sizeOfOnePart = sizeOfDataInBytes / DataAcquisition.DataContext.HowManyADC;
            for (i = 0, j = 0; i < sizeOfOnePart && j < DataAcquisition.DataContext.MaxBufferSize; i += 2, j++)
            {
                record = BitConverter.ToInt16(dataInBytes, i);
                ADC1_rawData[j] = record;
            }
            if (DataAcquisition.DataContext.HowManyADC > 1)
            {
                for (j = 0; i < 2*sizeOfOnePart && j < DataAcquisition.DataContext.MaxBufferSize; i += 2, j++)
                {
                    record = BitConverter.ToInt16(dataInBytes, i);
                    ADC2_rawData[j] = record;
                }
            }
            if (DataAcquisition.DataContext.HowManyADC == 3)
            {
                for (j = 0; i < sizeOfDataInBytes && j < DataAcquisition.DataContext.MaxBufferSize; i += 2, j++)
                {
                    record = BitConverter.ToInt16(dataInBytes, i);
                    ADC3_rawData[j] = record;
                }
            }
            return true;
        }
        
        private void ErrorHandler(object sender, SerialErrorReceivedEventArgs e)
        {
            MessageBox.Show(e.EventType.ToString(), "Connection error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
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
            try
            {
                serialPort.Open();
                if (!serialPort.IsOpen)
                {
                    throw new Exception();
                }
            }
            catch(Exception)
            {
                MessageBox.Show("Port nie został otworzony! Wybierz port jeszcze raz", "Błąd portu",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                ChooseAndOpenPort();
            }
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
                CreateDataFiles();

                serialPort.DiscardOutBuffer();
                serialPort.DiscardInBuffer();

                if(DataAcquisition.DataContext.Mode == DataAcquisition.DataContext.Modes.Continuous)
                {
                    DataAcquisition.DataContext.BufferSize = DataAcquisition.DataContext.MaxBufferSize;
                }

                ADC1_rawData = new Int16[DataAcquisition.DataContext.BufferSize];
                ADC2_rawData = new Int16[DataAcquisition.DataContext.BufferSize];
                ADC3_rawData = new Int16[DataAcquisition.DataContext.BufferSize];

                serialPort.WriteLine("ACQ:SRAT " + DataAcquisition.DataContext.Frequency.ToString("0.###"));
                serialPort.WriteLine("ACQ:POIN " + DataAcquisition.DataContext.BufferSize.ToString());

                serialPort.WriteLine("ROUT:ENAB " + DataAcquisition.DataContext.HowManyADC);

                Thread receiveDataThread = new Thread(ReceiveDataLoop);
                receiveDataThread.Start();

                if (DataAcquisition.DataContext.Mode == DataAcquisition.DataContext.Modes.Continuous)
                {
                    serialPort.WriteLine("RUN");
                }
                else
                {
                    serialPort.WriteLine("DIG");
                }
                
            }
            else
            {
                MessageBox.Show("Port nie jest otwarty! Wybierz port jeszcze raz", "Port nieotwarty", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }
            
        }

        private void StopMeasurements(object sender, RoutedEventArgs e)
        {
            serialPort.WriteLine("STOP");
            isStopped = true;
            
            if(receiveDataThread.IsAlive)
            {
                receiveDataThread.Join();
            }
            MeasurementsStop?.Invoke(this, new EventArgs());
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
                        WriteHeaders(streamWriter);
                    }
                    goto case 2;
                case 2:
                    using (var streamWriter = new System.IO.StreamWriter(fileName[1], false))
                    {
                        WriteHeaders(streamWriter);
                    }
                    goto case 1;
                case 1:
                    using (var streamWriter = new System.IO.StreamWriter(fileName[0], false))
                    {
                        WriteHeaders(streamWriter);
                    }
                    break;
                default:
                    throw new ArgumentException();
            
            }
            
        }

        private static void WriteHeaders(System.IO.StreamWriter streamWriter)
        {
            streamWriter.Write("mode," + DataAcquisition.DataContext.Mode.ToString("g") + "\n");
            streamWriter.Write("frequency," + DataAcquisition.DataContext.Frequency.ToString() + ",Hz\n\n");
        }

        private void ReceiveDataLoop()
        {
            serialPort.ReadTimeout = readTimeout;
            string isReady = String.Empty;

            if(DataAcquisition.DataContext.Mode==DataAcquisition.DataContext.Modes.Continuous)
            {
                int samplesCountPerReceipt = DataAcquisition.DataContext.BufferSize / 2;
            }
            else
            {
                int samplesCountPerReceipt = DataAcquisition.DataContext.BufferSize;
            }

            Thread saveToFileThread = new Thread(() => SaveToCSV());
            while (true)
            {
                serialPort.WriteLine("WAV:COMP?");
                try
                {
                    while ((isReady = serialPort.ReadExisting()) == String.Empty);
                    if (isReady.Contains("YES"))
                    {
                        serialPort.WriteLine("WAV:DATA?");
                        if (ReceiveDataBlock())
                        {
                            if(saveToFileThread.ThreadState== System.Threading.ThreadState.Running)
                            {
                                throw new Exception("Saving previous data to the file not completed");
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
            if(saveToFileThread.IsAlive)
            {
                saveToFileThread.Join();
            }
            MeasurementsStop?.Invoke(this, new EventArgs());
        }

        private void SaveToCSV()
        {
            switch (DataAcquisition.DataContext.HowManyADC)
            {
                case 3:
                    using (var streamWriter = new System.IO.StreamWriter(fileName[2], true))
                    {
                        var csvWriter = new CsvHelper.CsvWriter(streamWriter);
                        var records3 = ADC3_rawData.Take(DataAcquisition.DataContext.BufferSize);
                        csvWriter.WriteRecords(records3);
                    }
                    goto case 2;
                case 2:
                    using (var streamWriter = new System.IO.StreamWriter(fileName[1], true))
                    {
                        var csvWriter = new CsvHelper.CsvWriter(streamWriter);
                        var records2 = ADC2_rawData.Take(DataAcquisition.DataContext.BufferSize);
                        csvWriter.WriteRecords(records2);
                    }
                    goto case 1;
                case 1:
                    using (var streamWriter = new System.IO.StreamWriter(fileName[0], true))
                    {
                        var csvWriter = new CsvHelper.CsvWriter(streamWriter);
                        var records1 = ADC1_rawData.Take(DataAcquisition.DataContext.BufferSize);
                        csvWriter.WriteRecords(records1);
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