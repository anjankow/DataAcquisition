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

        public bool halfToBeWritten;
        public int sizeToSavePerOneArray;

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
            int j_startIndex = halfToBeWritten ? sizeToSavePerOneArray : 0;
            int sizeOfOnePart = sizeOfDataInBytes / DataAcquisition.DataContext.HowManyADC;
            for (i = 0, j = j_startIndex; i < sizeOfOnePart && j < DataAcquisition.DataContext.BufferSize; i += 2, j++)
            {
                record = BitConverter.ToInt16(dataInBytes, i);
                ADC1_rawData[j] = record;
            }
            if (DataAcquisition.DataContext.HowManyADC > 1)
            {
                for (j = j_startIndex; i < 2*sizeOfOnePart && j < DataAcquisition.DataContext.BufferSize; i += 2, j++)
                {
                    record = BitConverter.ToInt16(dataInBytes, i);
                    ADC2_rawData[j] = record;
                }
            }
            if (DataAcquisition.DataContext.HowManyADC == 3)
            {
                for (j = j_startIndex; i < sizeOfDataInBytes && j < DataAcquisition.DataContext.BufferSize; i += 2, j++)
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

                receiveDataThread = new Thread(ReceiveDataLoop);
                receiveDataThread.Start();

                if (DataAcquisition.DataContext.Mode == DataAcquisition.DataContext.Modes.Continuous)
                {
                    serialPort.WriteLine("RUN");
                }
                else
                {
                    serialPort.WriteLine("DIG");
                }
                MeasurementsStart?.Invoke(this, new EventArgs());
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
                    WriteHeaders(fileName[2]);
                    goto case 2;
                case 2:
                    WriteHeaders(fileName[1]);
                    goto case 1;
                case 1:
                    WriteHeaders(fileName[0]);
                    break;
                default:
                    throw new ArgumentException();
            }
            
        }

        private static void WriteHeaders(string fileName)
        {
            using (var streamWriter = new System.IO.StreamWriter(fileName, false))
            {
                streamWriter.Write("mode," + DataAcquisition.DataContext.Mode.ToString("g") + "\n");
                streamWriter.Write("frequency," + DataAcquisition.DataContext.Frequency.ToString() + ",Hz\n\n");
            }
        }
        
        private void ReceiveDataLoop()
        {
            serialPort.ReadTimeout = readTimeout;
            string isReady = String.Empty;

            sizeToSavePerOneArray = DataAcquisition.DataContext.Mode == DataAcquisition.DataContext.Modes.Continuous ?
                    DataAcquisition.DataContext.BufferSize / 2 : DataAcquisition.DataContext.BufferSize;
            halfToBeWritten = false;

            Thread[] saveToFileThreads = new Thread[3];
            saveToFileThreads[0] = new Thread(() => SaveToCSV(fileName[0], ADC1_rawData));
            saveToFileThreads[1] = new Thread(() => SaveToCSV(fileName[1], ADC2_rawData));
            saveToFileThreads[2] = new Thread(() => SaveToCSV(fileName[2], ADC3_rawData));

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
                            foreach (var thread in saveToFileThreads)
                            {
                                if (thread.ThreadState == System.Threading.ThreadState.Running)
                                {
                                    throw new Exception("Saving previous data to the file not completed");
                                }
                            }
                            StartThreads(saveToFileThreads);
                            if (DataAcquisition.DataContext.Mode == DataAcquisition.DataContext.Modes.SingleShot)
                            {
                                break;
                            }
                            halfToBeWritten = !halfToBeWritten;
                        }
                        else
                        {
                            throw new Exception("Receive data block failure");
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
            foreach(var thread in saveToFileThreads)
            {
                if (thread.IsAlive)
                {
                    thread.Join();
                }
            }
            MeasurementsStop?.Invoke(this, new EventArgs());
        }

        private static void StartThreads(Thread[] saveToFileThreads)
        {
            saveToFileThreads[0].Start();
            if (DataAcquisition.DataContext.HowManyADC > 1)
            {
                saveToFileThreads[1].Start();
                if (DataAcquisition.DataContext.HowManyADC == 3)
                {
                    saveToFileThreads[2].Start();
                }
            }
        }

        private void SaveToCSV(string fileName, short[] rawData)
        {
            int skipValue = halfToBeWritten ? 0 : sizeToSavePerOneArray;
            var records = rawData.Skip(skipValue).Take(sizeToSavePerOneArray);
            using (var streamWriter = new System.IO.StreamWriter(fileName, true))
            {
                var csvWriter = new CsvHelper.CsvWriter(streamWriter);
                csvWriter.WriteRecords(records);
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