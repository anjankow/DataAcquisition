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
using System.IO.Ports;

namespace DataAcquisition
{
    /// <summary>
    /// Interaction logic for PortChoiceWindow.xaml
    /// </summary>
    public partial class PortChoiceWindow : Window
    {
        public PortChoiceWindow()
        {
            InitializeComponent();
            foreach(var portName in SerialPort.GetPortNames())
            {
                listView_ports.Items.Add(portName);
            }
        }

        private void Btn_reload_Click(object sender, RoutedEventArgs e)
        {
            listView_ports.Items.Clear();
            foreach (var portName in SerialPort.GetPortNames())
            {   
                listView_ports.Items.Add(portName);
            }
            if(listView_ports.HasItems)
            {
                listView_ports.SelectedItem = listView_ports.Items.GetItemAt(0);
            }
        }

        private void ListView_ports_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(listView_ports.SelectedItem != null)
            {
                btn_select.Visibility = Visibility.Visible;
            }
            else
            {
                btn_select.Visibility = Visibility.Hidden;
            }
        }

        private void Btn_select_Click(object sender, RoutedEventArgs e)
        {
            DataAcquisition.DataContext.Port = listView_ports.SelectedItem.ToString();
            Close();
        }

        
    }
}
