using System.IO.Ports;
using System.Net.Sockets;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Drawing;
using System.Windows.Media;
using System.Text;
namespace UARTPort
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 


    public partial class MainWindow : Window
    {
        private ICommunication communication;
        private static Timer netWorkConnectTimer;
        public MainWindow()
        {
            InitializeComponent();
            InitPortData();
        }
      
        //串口----------------------------------------------------------------------------

        //刷新串口
        private void Button_ClickReflashPort(object sender, RoutedEventArgs e)
            {
                cbxProtNumber.Items.Clear();
                string[] ports = SerialPort.GetPortNames();
                foreach (string port in ports)
                {
                    cbxProtNumber.Items.Add(port);
                }
                cbxProtNumber.SelectedIndex = 0;
            }

        //打开串口
        private void Button_ClickOpenPort(object sender, RoutedEventArgs e)
        {
            switch (btnOpenPort.Content.ToString() ?? string.Empty)
            {
                case "打开串口":

                    string PortNumbers = cbxProtNumber.Text;

                    int baudrate = Convert.ToInt32(cbxBaudRate.Text);

                    System.IO.Ports.Parity parity = cbxParity.Text == "None" ? System.IO.Ports.Parity.None :
                                                    cbxParity.Text == "Odd" ? System.IO.Ports.Parity.Odd :
                                                    cbxParity.Text == "Even" ? System.IO.Ports.Parity.Even :
                                                    cbxParity.Text == "Mark" ? System.IO.Ports.Parity.Mark :
                                                                                System.IO.Ports.Parity.Space;
                    int databits = Convert.ToInt32(cbxDataBit.Text);

                    System.IO.Ports.StopBits stopbits = cbxStopBit.Text == "None" ? System.IO.Ports.StopBits.None :
                                                        cbxStopBit.Text == "One" ? System.IO.Ports.StopBits.One :
                                                        cbxStopBit.Text == "Two" ? System.IO.Ports.StopBits.Two :
                                                                                    System.IO.Ports.StopBits.OnePointFive;

                    SerialPortHelper_DataReceived serialCallback = new SerialPortHelper_DataReceived(SerialPort_DataReceived);

                    communication = new SerialPortHelper(PortNumbers, baudrate, parity, databits, stopbits, serialCallback);

                    communication.Connect();


                    //订阅事件
                    //serialPortHelper.serialPort.DataReceived += SerialPort_DataReceived;

                        //更改按钮文本
                        btnOpenPort.Content = "关闭串口";

                        break;
                    case "关闭串口"://若已打开则关闭

                        //更改按钮文本
                        btnOpenPort.Content = "打开串口";
                        communication.Disconnect();

                        break;
                    default:
                        break;
            }
        }

        public delegate void SerialPortHelper_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e);
        //串口接收事件
        private void SerialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            Dispatcher.Invoke(new Action(() =>
            {
                switch (btnSwtichOutputCode.Content.ToString() ?? string.Empty.ToUpper())
                {
                    case "ASCII":
                        string data = sp.ReadExisting();
                        txtOutputBox.Text += data;
                        txtOutputBox.ScrollToEnd();
                        break;
                    case "HEX":
                        int byteToRead = sp.BytesToRead;
                        byte[] buffer = new byte[byteToRead];
                        sp.Read(buffer, 0, byteToRead);
                        txtOutputBox.Text += " " + BitConverter.ToString(buffer).Replace("-", " ");
                        txtOutputBox.ScrollToEnd();
                        break;
                    default:
                        break;
                }

            }));

        }


        //重写回车
        private void txtInputData_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnSendPortData_Click(sender, e);   
            }
        }
        //--------------------------------------------------------------------------------



        //通用----------------------------------------------------------------------------

        //初始化网口和串口控件参数
        public void InitPortData()
        {
                //cbxPortNumber
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                cbxProtNumber.Items.Add(port);
            }
            //BautRate
            cbxBaudRate.Items.Add(1200);
            cbxBaudRate.Items.Add(2400);
            cbxBaudRate.Items.Add(4800);
            cbxBaudRate.Items.Add(9600);
            cbxBaudRate.Items.Add(19200);
            cbxBaudRate.Items.Add(38400);
            cbxBaudRate.Items.Add(57600);
            cbxBaudRate.Items.Add(115200);

            //DataBit
            cbxDataBit.Items.Add(5);
            cbxDataBit.Items.Add(6);
            cbxDataBit.Items.Add(7);
            cbxDataBit.Items.Add(8);

            //StopBit

            cbxStopBit.Items.Add("None");
            cbxStopBit.Items.Add("One");
            cbxStopBit.Items.Add("Two");
            cbxStopBit.Items.Add("OnePointFive");

            //Parity
            cbxParity.Items.Add("None");
            cbxParity.Items.Add("Odd");
            cbxParity.Items.Add("Even");
            cbxParity.Items.Add("Mark");
            cbxParity.Items.Add("Space");

            //NetWorkProtocolType
            cbxProtocolType.Items.Add("Tcp Client");
            cbxProtocolType.Items.Add("Tcp Servers");
            cbxProtocolType.Items.Add("UDP");

            //NetWorkRemoteHostAddress
            cbxRemoteHostAddress.Items.Add("127.0.0.1");
            //本机IP
            string hostName = Dns.GetHostName();

            IPAddress[] addresses = Dns.GetHostAddresses(hostName);

            foreach (IPAddress ip in addresses)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    cbxRemoteHostAddress.Items.Add(ip.ToString());
                }
            }
            //初始化网口和串口控件参数
        }

        //切换输出编码
        private void Button_ClickOuttputCode(object sender, RoutedEventArgs e)
        {
            switch (btnSwtichOutputCode.Content.ToString() ?? string.Empty.ToUpper())
            {
                case "ASCII":
                    btnSwtichOutputCode.Content = "HEX";
                    break;
                case "HEX":
                    btnSwtichOutputCode.Content = "ASCII";
                    break;
                default:
                    break;
            }
        }

        //切换输入编码
        private void btnSwtichInputCode_Click(object sender, RoutedEventArgs e)
        {
            switch (btnSwtichInputCode.Content.ToString() ?? string.Empty.ToUpper())
            {
                case "ASCII":
                    btnSwtichInputCode.Content = "HEX";
                    chkAddChangeLine.IsEnabled = false;
                    break;
                case "HEX":
                    btnSwtichInputCode.Content = "ASCII";
                    chkAddChangeLine.IsEnabled = true;

                    break;
                default:
                    break;
            }
        }

        //发送事件
        private void btnSendPortData_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = chkAddChangeLine.IsChecked.GetValueOrDefault();//新行
            try
            {
                switch (btnSwtichInputCode.Content.ToString() ?? string.Empty.ToUpper())
                {
                    case "ASCII":
                        if (isChecked)
                        {
                            if (communication.isConnected)
                            {
                                communication.WriteData(txtInputData.Text + "\r\n");
                            }
                            if (communication != null && communication.ConnectType == "Tcp Client")
                            {
                                communication.WriteData(txtInputData.Text + "\r\n");
                            }
                            if (communication != null && communication.ConnectType == "UDP")
                            {
                                communication.WriteData(txtInputData.Text + "\r\n",txtUdpRemoteHostAdress.Text,Convert.ToInt32(txtRemoteHostPort.Text));
                            }

                    }
                        else
                        {
                            if (communication.isConnected && communication.ConnectType == "Serial")
                            {
                                communication.WriteData(txtInputData.Text);
                            }
                            if (communication != null && communication.ConnectType == "Tcp Client")
                            {
                                communication.WriteData(txtInputData.Text);
                            }
                            if (communication != null && communication.ConnectType == "UDP")
                            {
                                communication.WriteData(txtInputData.Text, txtUdpRemoteHostAdress.Text, Convert.ToInt32(txtRemoteHostPort.Text));
                            }
                    }
                        break;
                    case "HEX":
                        byte[] bytesToSend = communication.HexStringToByteArray(txtInputData.Text);

                        if (communication != null && (communication.ConnectType == "Serial" || communication.ConnectType == "Tcp Client")) 
                        {
                            communication.WriteHexData(bytesToSend, 0, bytesToSend.Length);
                        }
                        if (communication != null && communication.ConnectType == "UDP")
                        {
                            communication.WriteHexData(bytesToSend, txtUdpRemoteHostAdress.Text, Convert.ToInt32(txtRemoteHostPort.Text));
                        }
                            break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
        //--------------------------------------------------------------------------------

        //NetWork----------------------------------------------------------------------------

        //协议选择
        private void cbxProtocolType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            switch (cbxProtocolType.SelectedItem.ToString())
            {
                case "Tcp Client":
                    btnStartNet.Content = "连接";
                    lblHostAddressText.Content = "远程主机地址";
                    lblHostPortText.Content = "远程主机端口";
                    lblUdpRemoteHostAddress.Visibility = Visibility.Hidden;
                    txtUdpRemoteHostAdress.Visibility = Visibility.Hidden;
                    RemoveIpForZero();
                    break;
                case "Tcp Servers":
                    btnStartNet.Content = "打开";
                    lblHostAddressText.Content = "本地主机地址";
                    lblHostPortText.Content = "本地主机端口";
                    lblUdpRemoteHostAddress.Visibility = Visibility.Hidden;
                    txtUdpRemoteHostAdress.Visibility = Visibility.Hidden;
                    RemoveIpForZero();
                    break;
                case "UDP":
                    btnStartNet.Content = "打开";
                    lblHostAddressText.Content = "本地主机地址";
                    lblHostPortText.Content = "本地主机端口";
                    lblUdpRemoteHostAddress.Visibility = Visibility.Visible;
                    txtUdpRemoteHostAdress.Visibility = Visibility.Visible; 
                    cbxRemoteHostAddress.Items.Add("0.0.0.0");
                    break;
                default:
                    btnStartNet.Content = "连接";
                    break;
            }
        }

        //Remove0.0.0.0
        public void RemoveIpForZero()
        {
            int index = cbxRemoteHostAddress.Items.IndexOf("0.0.0.0");

            if (index >= 0)
            {
                cbxRemoteHostAddress.Items.RemoveAt(index);
            }
        }

        public void InitNetWork() 
        {
            try
            {
                UdpReceiveDelegate udpReceive = new UdpReceiveDelegate(UdpReceiveCallBackMethod);
                communication = new NetWorkHelper(udpReceive);
                communication.Connect(cbxProtocolType.Text, cbxRemoteHostAddress.Text, Convert.ToInt32(txtRemoteHostPort.Text));
                netWorkConnectTimer = new Timer(NewWorkConnectStateCheckTimerCallback, null, 0, 1000);

                if (communication.isConnected)
                {
                    btnStartNet.Content = "关闭";
                }
                //communication.UdpReceiveNotify += NetWorkHelper_UdpReceiveNotify;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            
        }

     
        //StartNet事件
        private void btnStartNet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (communication==null)
                {
                    InitNetWork();
                    return;
                }

                if (communication.isConnected)
                {
                    communication.Disconnect();
                    lblNetWorkLinkState.Content = "已断开";
                    txtOutputBox.Text += "已断开\r\n";
                    lblNetWorkLinkState.Background = Brushes.White;
                    switch (cbxProtocolType.Text)
                    {
                        case "Tcp Client":
                            btnStartNet.Content = "连接";
                            break;
                        case "Tcp Servers":
                            btnStartNet.Content = "打开";
                            break;
                        case "UDP":
                            btnStartNet.Content = "打开";
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    InitNetWork();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        //连接状态检测
        int ConnectCount = 0;

            

        private  void NewWorkConnectStateCheckTimerCallback(object state)
        {
            ConnectCount++;
            Dispatcher.Invoke(new Action(() => { txtOutputBox.Text += string.Format("{0} 正在第{1}次检测连接\r\n", DateTime.Now, ConnectCount); }));
            if (communication.isConnected)
            {
                Dispatcher.Invoke(new Action(() => 
                { 
                    txtOutputBox.Text += "连接成功\r\n";
                    lblNetWorkLinkState.Content = "已连接";
                    lblNetWorkLinkState.Background = Brushes.Green;
                    switch (cbxProtocolType.Text)
                    {
                        case "Tcp Client":
                            btnStartNet.Content = "断开";
                            break;
                        case "Tcp Servers":
                            btnStartNet.Content = "关闭";
                            break;
                        default:
                            break;
                    }
                }));
                ConnectCount = 0; 
                netWorkConnectTimer.Dispose();

            }
            if (ConnectCount >= 3)
            {
                netWorkConnectTimer.Dispose();
                ConnectCount = 0;
                Dispatcher.Invoke(new Action(() => 
                { 
                    txtOutputBox.Text += "连接超时\r\n";
                }));
            }
        }

        public delegate void UdpReceiveDelegate(byte[] receiveBytes);

        //UDP接收数据回调函数
        public void UdpReceiveCallBackMethod(byte[] receiveBytes)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                switch (btnSwtichOutputCode.Content.ToString() ?? string.Empty.ToUpper())
                {
                    case "ASCII":
                        string receiveString = Encoding.ASCII.GetString(receiveBytes);
                        txtOutputBox.Text += receiveString;
                        txtOutputBox.ScrollToEnd();
                        break;
                    case "HEX":
                        txtOutputBox.Text += " " + BitConverter.ToString(receiveBytes).Replace("-", " ");
                        txtOutputBox.ScrollToEnd();
                        break;
                    default:
                        break;
                }
            }));
               // Dispatcher.Invoke(new Action(() => { txtOutputBox.Text += value; }));
        }
    }
    //--------------------------------------------------------------------------------
}