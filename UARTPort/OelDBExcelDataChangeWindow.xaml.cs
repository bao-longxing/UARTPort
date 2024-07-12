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

namespace UARTPort
{
    /// <summary>
    /// OelDBExcelDataChangeWindow.xaml 的交互逻辑
    /// </summary>
    public partial class OelDBExcelDataChangeWindow : Window
    {
        public OelDBExcelDataChangeWindow()
        {
            InitializeComponent();
            btnCancel.Click += BtnCancel_Click;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        public void SetUIValue(SerialData serial) 
        {
            txtCountShouldBeChanged.Text = serial.Count.ToString();
            txtPortNumberShouldBeChanged.Text = serial.PortNumber.ToString();
            txtDateShouldBeChanged.Text = serial.Date.ToString();
            txtStateShouldBeChanged.Text = serial.State.ToString();
            txtDataShouldBeChanged.Text = serial.Data.ToString();
        }
    }
    public class Test
    {
        MainWindow MainWindow { get; set; }
        public Test(MainWindow mainWindow)
        {
            MainWindow = mainWindow;
            mainWindow.btnOpenPort.Click += BtnOpenPort_Click;
        }

        private void BtnOpenPort_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Test Succses");
        }
    }
}
