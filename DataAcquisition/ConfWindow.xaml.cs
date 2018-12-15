using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DataAcquisition
{
    /// <summary>
    /// Interaction logic for ConfWindow.xaml
    /// </summary>
    public partial class ConfWindow : Window
    {
        private bool freqCorrect, bufferSizeCorrect, bufferNumCorrect;

        public ConfWindow()
        {
            InitializeComponent();
            freqCorrect = true;
            bufferNumCorrect = true;
            bufferSizeCorrect = true;
            txtBox_bufferSize.Text = DataAcquisition.DataContext.BufferSize.ToString();
            txtBox_frequency.Text = DataAcquisition.DataContext.Frequency.ToString();
            slid_bufferSize.Value = DataAcquisition.DataContext.BufferSize;
            slid_frequency.Value = DataAcquisition.DataContext.Frequency;
            RadioButton[] rbtns = { rbtn_1, rbtn_2, rbtn_3 };
            foreach (var rbtn in rbtns)
            {
                if (DataAcquisition.DataContext.HowManyADC.ToString().Contains(rbtn.Content.ToString()))
                {
                    rbtn.IsChecked = true;
                }
            }
            rbtn_continMeas.IsChecked = DataAcquisition.DataContext.Mode == DataAcquisition.DataContext.Modes.Continuous;
            rbtn_singleShot.IsChecked = DataAcquisition.DataContext.Mode == DataAcquisition.DataContext.Modes.SingleShot;
            lbl_wrongBufSize.Visibility = Visibility.Hidden;
            lbl_wrongFreq.Visibility = Visibility.Hidden;
            btn_OK.IsEnabled = false;
            txtBox_bufferSize.TextChanged += new TextChangedEventHandler(TxtBox_bufferSize_TextChanged);
            txtBox_frequency.TextChanged += new TextChangedEventHandler(TxtBox_frequency_TextChanged);
        }

        private void SetButtonOkState()
        {
            btn_OK.IsEnabled = (freqCorrect && bufferNumCorrect && bufferSizeCorrect) ? true : false;
        }

        private void Btn_OK_Click(object sender, RoutedEventArgs e)
        {
            DataAcquisition.DataContext.BufferSize = int.Parse(txtBox_bufferSize.Text);
            DataAcquisition.DataContext.Frequency = double.Parse(txtBox_frequency.Text);
            if (rbtn_1.IsChecked == true)
            {
                DataAcquisition.DataContext.HowManyADC = 1;
            }
            if (rbtn_2.IsChecked == true)
            {
                DataAcquisition.DataContext.HowManyADC = 2;
            }
            if (rbtn_3.IsChecked == true)
            {
                DataAcquisition.DataContext.HowManyADC = 3;
            }
            this.Close();
        }

        private void Slid_bufferSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            txtBox_bufferSize.Text = ((int)slid_bufferSize.Value).ToString();
        }

        private void TxtBox_bufferSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(txtBox_bufferSize.Text, out int userBufferSize) && 
                userBufferSize <= DataAcquisition.DataContext.MaxBufferSize && userBufferSize >= DataAcquisition.DataContext.MinBufferSize)
            {
                lbl_wrongBufSize.Visibility = Visibility.Hidden;
                bufferSizeCorrect = true;
            }
            else
            {
                lbl_wrongBufSize.Visibility = Visibility.Visible;
                bufferSizeCorrect = false;
            }
            SetButtonOkState();
        }

        private void Btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        

        private void Rbtn_continMeas_Checked(object sender, RoutedEventArgs e)
        {
            DataAcquisition.DataContext.Mode = DataAcquisition.DataContext.Modes.Continuous;
            txtBox_bufferSize.IsEnabled = false;
            slid_bufferSize.IsEnabled = false;
        }

        private void Rbtn_singleShot_Checked(object sender, RoutedEventArgs e)
        {
            DataAcquisition.DataContext.Mode = DataAcquisition.DataContext.Modes.SingleShot;
            txtBox_bufferSize.IsEnabled = true;
            slid_bufferSize.IsEnabled = true;
        }

        private void Slid_frequency_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            txtBox_frequency.Text = slid_frequency.Value.ToString("######0.00");
        }

        private void TxtBox_frequency_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(txtBox_frequency.Text, out double userFrequency) && userFrequency <= DataAcquisition.DataContext.MaxFrequency && userFrequency >= DataAcquisition.DataContext.MinFrequency)
            {
                lbl_wrongFreq.Visibility = Visibility.Hidden;
                freqCorrect = true;
            }
            else
            {
                lbl_wrongFreq.Visibility = Visibility.Visible;
                freqCorrect = false;
            }
            SetButtonOkState();
        }
    }
}
