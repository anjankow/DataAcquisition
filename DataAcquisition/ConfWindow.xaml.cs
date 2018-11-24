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
            txtBox_howManyBuffers.Text = DataAcquisition.DataContext.HowManyBuffers.ToString();
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
            lbl_wrongBufSize.Visibility = Visibility.Hidden;
            lbl_wrongFreq.Visibility = Visibility.Hidden;
            lbl_wrongBuffNum.Visibility = Visibility.Hidden;
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
            DataAcquisition.DataContext.Frequency = int.Parse(txtBox_frequency.Text);
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
            DataAcquisition.DataContext.HowManyBuffers = int.Parse(txtBox_howManyBuffers.Text);
            this.Close();
        }

        private void Slid_bufferSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            txtBox_bufferSize.Text = ((int)slid_bufferSize.Value).ToString();
        }

        private void TxtBox_bufferSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(txtBox_bufferSize.Text, out int userBufferSize) && userBufferSize <= 16000 && userBufferSize >= 2)
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

        private void TxtBox_howManyBuffers_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(int.TryParse(txtBox_howManyBuffers.Text, out int userBufferNum) && userBufferNum > 0)
            {
                bufferNumCorrect = true;
                lbl_wrongBuffNum.Visibility = Visibility.Hidden;
            }
            else
            {
                bufferNumCorrect = false;
                lbl_wrongBuffNum.Visibility = Visibility.Visible;
            }
            SetButtonOkState();
        }

        private void Slid_frequency_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            txtBox_frequency.Text = ((int)slid_frequency.Value).ToString();
        }

        private void TxtBox_frequency_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(txtBox_frequency.Text, out int userFrequency) && userFrequency <= 1000000 && userFrequency >= 1000)
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
