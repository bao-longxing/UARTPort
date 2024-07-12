using System.IO.Ports;
using System.Net.Sockets;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Drawing;
using System.Windows.Media;
using System.Text;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Data;
using NPOI.SS.Formula.Functions;
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
        private FileIOMannager fileIOMannager;
        private FileIOFactor fileFactor;
        public MainWindow()
        {
            InitializeComponent();
            InitPortAndNetWorkData();
            //File
            fileFactor = new FileIOFactor();
            this.Closing += MainWindow_Closing;
            
        }

        //在窗口关闭前关闭文件类避免保存失败
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (fileIOMannager!=null)
            {
                fileIOMannager.Close();
            }
        }


        //串口相关--------------------------------------------------------------------------------------------------------------------------------------------------------
        
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

                    InitFileIO();//文件IO

                    SerialUIDisable();
                    break;
                case "关闭串口":
                    fileIOMannager.Close();
                    fileIOMannager = null;
                    SerialUIEnable();
                    communication.Disconnect();
                    break;
                default:
                    break;
            }
        }
        
        //串口控件
        public void SerialUIEnable() 
        {
            btnOpenPort.Content = "打开串口";
            btnReflashPort.IsEnabled = true;
            cbxParity.IsEnabled = true;
            cbxDataBit.IsEnabled = true;
            cbxStopBit.IsEnabled = true;
            cbxBaudRate.IsEnabled = true;
            cbxProtNumber.IsEnabled = true;
            rdoUseCvs.IsEnabled = true;
            rdoUseExcel.IsEnabled = true;
            rdoUseTxt.IsEnabled = true;
            btnStartNet.IsEnabled = true;
        }
        
        //串口控件
        public void SerialUIDisable() 
        {
            btnOpenPort.Content = "关闭串口";
            btnReflashPort.IsEnabled = false;
            cbxParity.IsEnabled = false;
            cbxDataBit.IsEnabled = false;
            cbxStopBit.IsEnabled = false;
            cbxBaudRate.IsEnabled = false;
            cbxProtNumber.IsEnabled=false; 
            rdoUseTxt.IsEnabled = false;
            rdoUseCvs.IsEnabled = false;
            rdoUseExcel .IsEnabled = false;
            btnStartNet.IsEnabled = false;
        }
       
        //串口事件委托
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
                        WriteSerialDataLogToFile(data,"Receive");
                        break;
                    case "HEX":
                        int byteToRead = sp.BytesToRead;
                        byte[] buffer = new byte[byteToRead];
                        sp.Read(buffer, 0, byteToRead);
                        string value_str = " " + BitConverter.ToString(buffer).Replace("-", " ");
                        txtOutputBox.Text += value_str;
                        txtOutputBox.ScrollToEnd();
                        WriteSerialDataLogToFile(value_str, "Receive");
                        break;
                    default:
                        break;
                }

            }));

        }
        
        //串口日志写入文件
        public void WriteSerialDataLogToFile(string value,string Send_ReceiveStateFlag) 
        {
            string writeValue = "ReceivePort="  + cbxProtNumber.Text + "\r\n"
                                + "Date=" + DateTime.Now + "\r\n"
                                + "State=" + Send_ReceiveStateFlag + "\r\n"
                                + "Data=" + value + "\r\n"; //文件IO
            fileIOMannager.Write(writeValue);
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------


        //网口相关------------------------------------------------------------------------------------------------------------------------------------------------------------

        //基本功能
        int ConnectCount = 0;//连接检测次数

        //初始化网口类
        public void InitNetWork()
        {
            try
            {
                //接收函数回调
                NetWorkReceiveDelegate netWorkReceive = new NetWorkReceiveDelegate(NetWorkReceiveCallBackMethod);
                //连接状态回调
                NetWorkConnectCallback netWorkConnectStateCallback = NetWorkConnectStateCallbackFunction;

                communication = new NetWorkHelper(netWorkReceive);

                communication.SetNetWorkCallback(netWorkConnectStateCallback);

                //连接状态检测
                netWorkConnectTimer = new Timer(NewWorkConnectStateCheckTimer, null, 0, 1000);

                //控件
                txtOutputBox.Text += string.Format("{0} 连接中....\r\n", DateTime.Now, ConnectCount);
                btnStartNet.IsEnabled = false;


                communication.Connect(cbxProtocolType.Text, cbxRemoteHostAddress.Text, Convert.ToInt32(txtRemoteHostPort.Text));




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

        //断开连接
        public void Disconnenct()
        {
            communication.Disconnect();
            NetWorkUIForDisconnected();
        }

        //连接检测
        private void NewWorkConnectStateCheckTimer(object state)
        {
            ConnectCount++;

            if (communication.isConnected)
            {
                ConnectCount = 0;
                netWorkConnectTimer.Dispose();

            }
            if (ConnectCount >= 3)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    btnStartNet.IsEnabled = true;
                    txtOutputBox.Text += "连接超时\r\n";
                }));
                ConnectCount = 0;
                netWorkConnectTimer.Dispose();
            }
        }

        //UI

        //已连接时的UI设置
        public void NetWorkUIForConnected()
        {
            txtOutputBox.Text += "连接成功\r\n";
            btnStartNet.IsEnabled = true;
            cbxProtocolType.IsEnabled = false;
            cbxRemoteHostAddress.IsEnabled = false;
            txtRemoteHostPort.IsEnabled = false;
            lblNetWorkLinkState.Content = "已连接";
            lblNetWorkLinkState.Background = System.Windows.Media.Brushes.Green;
            rdoUseCvs.IsEnabled = false;
            rdoUseExcel.IsEnabled = false;
            rdoUseTxt.IsEnabled = false;
            btnOpenPort.IsEnabled = false;
            switch (cbxProtocolType.Text)
            {
                case "Tcp Client":
                    btnStartNet.Content = "断开";
                    break;
                case "Tcp Servers":
                    btnStartNet.Content = "关闭";
                    lblTcpServersClient.Visibility = Visibility.Visible;
                    cbxAddressOfClient.Visibility = Visibility.Visible;
                    break;
                case "UDP":
                    btnStartNet.Content = "关闭";
                    break;
                default:
                    break;
            }
        }

        //断开连接时的UI设置
        public void NetWorkUIForDisconnected()
        {
            lblNetWorkLinkState.Content = "已断开";
            txtOutputBox.Text += "已断开\r\n";
            cbxProtocolType.IsEnabled = true;
            cbxRemoteHostAddress.IsEnabled = true;
            txtRemoteHostPort.IsEnabled = true;
            lblNetWorkLinkState.Background = System.Windows.Media.Brushes.White;
            rdoUseCvs.IsEnabled = true;
            rdoUseExcel.IsEnabled = true;
            rdoUseTxt.IsEnabled = true;
            btnOpenPort.IsEnabled = true;
            switch (cbxProtocolType.Text)
            {
                case "Tcp Client":
                    btnStartNet.Content = "连接";
                    break;
                case "Tcp Servers":
                    btnStartNet.Content = "打开";
                    lblTcpServersClient.Visibility = Visibility.Hidden;
                    cbxAddressOfClient.Visibility = Visibility.Hidden;
                    break;
                case "UDP":
                    btnStartNet.Content = "打开";
                    break;
                default:
                    break;
            }
        }
        //使用Client时的UI设置
        public void SetUIForTcpClient()
        {
            btnStartNet.Content = "连接";
            lblHostAddressText.Content = "远程主机地址";
            lblHostPortText.Content = "远程主机端口";
            lblUdpRemoteHostAddress.Visibility = Visibility.Hidden;
            txtUdpRemoteHostAdress.Visibility = Visibility.Hidden;
            lblTcpServersClient.Visibility = Visibility.Hidden;
            cbxAddressOfClient.Visibility = Visibility.Hidden;
            RemoveIpForZero();
        }

        //使用Servers时的UI设置
        public void SetUIForTcpServers()
        {
            btnStartNet.Content = "打开";
            lblHostAddressText.Content = "本地主机地址";
            lblHostPortText.Content = "本地主机端口";
            lblUdpRemoteHostAddress.Visibility = Visibility.Hidden;
            txtUdpRemoteHostAdress.Visibility = Visibility.Hidden;
            lblTcpServersClient.Visibility = Visibility.Visible;
            cbxAddressOfClient.Visibility = Visibility.Visible;
            RemoveIpForZero();
        }

        //使用UDP时的UI设置
        public void SetUIForUDP()
        {
            btnStartNet.Content = "打开";
            lblHostAddressText.Content = "本地主机地址";
            lblHostPortText.Content = "本地主机端口";
            lblUdpRemoteHostAddress.Visibility = Visibility.Visible;
            txtUdpRemoteHostAdress.Visibility = Visibility.Visible;
            lblTcpServersClient.Visibility = Visibility.Hidden;
            cbxAddressOfClient.Visibility = Visibility.Hidden;
            cbxRemoteHostAddress.Items.Add("0.0.0.0");
        }

        //移除Ip项0.0.0.0
        public void RemoveIpForZero()
        {
            // Remove IP address "0.0.0.0" if exists
            if (cbxRemoteHostAddress.Items.Contains("0.0.0.0"))
            {
                cbxRemoteHostAddress.Items.Remove("0.0.0.0");
            }
        }

        //协议选择事件
        private void cbxProtocolType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string selectedItem = cbxProtocolType.SelectedItem.ToString();

            switch (selectedItem)
            {
                case "Tcp Client":
                    SetUIForTcpClient();
                    break;
                case "Tcp Servers":
                    SetUIForTcpServers();
                    break;
                case "UDP":
                    SetUIForUDP();
                    break;
                default:
                    btnStartNet.Content = "连接";
                    break;
            }
        }

        //回调、事件及委托

        //网口回调委托
        public delegate void NetWorkConnectCallback();

        //网口连接状态回调
        public void NetWorkConnectStateCallbackFunction()
        {
            NetWorkUIForConnected();
        }

        //接收回调委托
        public delegate void NetWorkReceiveDelegate(byte[] receiveBytes);

        //接收数据回调
        public void NetWorkReceiveCallBackMethod(byte[] receiveBytes)
        {
            string tempStringValue = Encoding.ASCII.GetString(receiveBytes);

            //Tcp Client 下线
            if (tempStringValue.Contains("Servers: Client Offline"))
            {
                //避免通知消息被转换为HEX
                btnSwtichOutputCode.Content = "ASCII";
                string searchValue = "Client:  ";
                int strIndex = tempStringValue.IndexOf(searchValue);
                string endPointString = tempStringValue.Substring(strIndex + 9);
                cbxAddressOfClient.Items.Remove(endPointString);
            }

            //Tcp Client 上线
            if (tempStringValue.Contains("Servers: Client Online"))
            {
                btnSwtichOutputCode.Content = "ASCII";
                if (cbxAddressOfClient.SelectedIndex == -1)
                {
                    cbxAddressOfClient.SelectedIndex = 0;
                }
                string searchValue = "Client:  ";
                int strIndex = tempStringValue.IndexOf(searchValue);
                string endPointString = tempStringValue.Substring(strIndex + 9);
                cbxAddressOfClient.Items.Add(endPointString);
            }

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
        }

        //连接按钮事件
        private void btnStartNet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (communication == null)
                {
                    InitNetWork();
                    InitFileIO();
                    return;
                }

                if (communication.isConnected)
                {
                    fileIOMannager.Close();
                    Disconnenct();
                }
                else
                {
                    InitFileIO();
                    InitNetWork();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        //----------------------------------------------------------------------------------------------------------------------------------------------------------------


        //文件相关----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        bool IsOnlyRead = false;//只读打开标志用于在没有打开任何连接时初始化文件类
        ObservableCollection<SerialData>? serialDatas;
        OelDBExcelDataChangeWindow changeWindow;
        bool IsUpdate = false;//区分更新和添加
        public void HideListView()
        {
            lvOleData.Visibility = Visibility.Hidden;
        }
        public void ShowListView()
        {
            //Style
            GridView gridView = new GridView();
            double width = txtOutputBox.Width;
            double height = txtOutputBox.Height;
            lvOleData.Margin = new Thickness(270, 65, 50, 253);
            lvOleData.Width = width;
            lvOleData.Height = height;


            //添加列
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Count",
                DisplayMemberBinding = new Binding("Count"),
                Width = 50
            });
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "PortNumber",
                DisplayMemberBinding = new Binding("PortNumber"),
                Width = 100
            });
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Date",
                DisplayMemberBinding = new Binding("Date"),
                Width = 100
            });
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "State",
                DisplayMemberBinding = new Binding("State"),
                Width = 100
            });
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Data",
                DisplayMemberBinding = new Binding("Data"),
                Width = 1000
            });

            //添加右键菜单并绑定处理函数
            MenuItem editMenuAdd = new MenuItem { Header = "添加" };
            editMenuAdd.Click += EditMenuAdd_Click;

            MenuItem editMenuUpdate = new MenuItem { Header = "修改" };
            editMenuUpdate.Click += EditMenuUpdate_Click;

            MenuItem editMenuDelete = new MenuItem { Header = "删除" };
            editMenuDelete.Click += EditMenuDelete_Click;



            lvOleData.ContextMenu.Items.Add(editMenuAdd);
            lvOleData.ContextMenu.Items.Add(editMenuUpdate);
            lvOleData.ContextMenu.Items.Add(editMenuDelete);

            lvOleData.View = gridView;

            lvOleData.Visibility = Visibility.Visible;
        }

        private void EditMenuDelete_Click(object sender, RoutedEventArgs e)
        {
            
            if (lvOleData.SelectedIndex!=-1)
            {
                serialDatas.RemoveAt(lvOleData.SelectedIndex);
            }

            fileIOMannager.UpdateItem(serialDatas);

            MessageBox.Show("OK"); 
        }

        private void EditMenuUpdate_Click(object sender, RoutedEventArgs e)
        {
            IsUpdate = true;
            if (lvOleData.SelectedItem is SerialData serial)
            {
                changeWindow = new OelDBExcelDataChangeWindow();
                changeWindow.SetUIValue(serial);
                changeWindow.btnOK.Click += BtnOK_Click;
                changeWindow.ShowDialog();
            }
        }

        private void EditMenuAdd_Click(object sender, RoutedEventArgs e)
        {
            IsUpdate = false;
            changeWindow = new OelDBExcelDataChangeWindow();
            changeWindow.btnOK.Click += BtnOK_Click;
            changeWindow.ShowDialog();
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (IsUpdate==false)
                {
                    changeWindow.DialogResult = true;

                    SerialData serialData = new SerialData();
                    serialData.Count = Convert.ToInt32(changeWindow.txtCountShouldBeChanged.Text);
                    serialData.PortNumber = changeWindow.txtPortNumberShouldBeChanged.Text;
                    serialData.Date = changeWindow.txtDateShouldBeChanged.Text;
                    serialData.State = changeWindow.txtStateShouldBeChanged.Text;
                    serialData.Data = changeWindow.txtDataShouldBeChanged.Text;
                    serialDatas.Add(serialData);
                    fileIOMannager.UpdateItem(serialDatas);
                }
                if (IsUpdate==true)
                {
                    changeWindow.DialogResult = true;

                    SerialData serialData = new SerialData();
                    serialData.Count = Convert.ToInt32(changeWindow.txtCountShouldBeChanged.Text);
                    serialData.PortNumber = changeWindow.txtPortNumberShouldBeChanged.Text;
                    serialData.Date = changeWindow.txtDateShouldBeChanged.Text;
                    serialData.State = changeWindow.txtStateShouldBeChanged.Text;
                    serialData.Data = changeWindow.txtDataShouldBeChanged.Text;
                    serialDatas[(int)serialData.Count] = serialData;
                    fileIOMannager.UpdateItem(serialDatas);
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
           
        }


        //打开文件事件
        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (fileIOMannager == null)
                {
                    IsOnlyRead = true;
                    InitFileIO(IsOnlyRead);
                }
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.InitialDirectory = AppContext.BaseDirectory;
                switch (fileIOMannager.FileIOType)
                {
                    case "txt":
                        openFileDialog.Filter = "文本文件|*.txt|所有文件|*.*";
                        break;
                    case "csv":
                        openFileDialog.Filter = "CSV文件|*.csv|所有文件|*.*";
                        break;
                    case "excel":
                        openFileDialog.Filter = "Excel表格|*.xls;*.xlsx|所有文件|*.*";
                        break;
                    case "OELDB For Excel":
                        openFileDialog.Filter = "Excel表格|*.xls;*.xlsx|所有文件|*.*";
                        break;
                    default:
                        break;
                }
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == true)
                {
                    string selectedFileName = openFileDialog.FileName;
                    if (fileIOMannager.FileIOType== "OELDB For Excel")
                    {
                        ShowListView();

                        serialDatas = fileIOMannager.Read(selectedFileName, ref lvOleData);
                    }
                    else
                    {
                        txtOutputBox.Text = fileIOMannager.Read(selectedFileName);
                    }

                }
                if (IsOnlyRead && fileIOMannager.FileIOType!= "OELDB For Excel")
                {
                    fileIOMannager.Close();
                    fileIOMannager = null;
                    IsOnlyRead = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //初始化文件类
        public void InitFileIO(bool isReadOnly = false)
        {
            //文件IO
            string fileType = (bool)rdoUseCvs.IsChecked ? "csv" : (bool)rdoUseExcel.IsChecked ? "excel" : (bool)rdoUseTxt.IsChecked ? "txt": "OELDB For Excel";
            fileIOMannager = fileFactor.CreateFileIO(fileType);
            if (isReadOnly == false)
            {
                fileIOMannager.InitFileAddressAndStreamClass();
            }
        }

        //----------------------------------------------------------------------------------------------------------------------------------------------------------------


        //串口和网口公用方法--------------------------------------------------------------------------------------------------------------------------------------------------------
        
        //回车发送
        private void txtInputData_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnSendPortData_Click(sender, e);
            }
        }
        //初始化网口和串口控件参数
        public void InitPortAndNetWorkData()
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
            bool newRowIsChecked = chkAddChangeLine.IsChecked.GetValueOrDefault();//新行
            try
            {
                switch (btnSwtichInputCode.Content.ToString() ?? string.Empty.ToUpper())
                {
                    case "ASCII":
                        if (newRowIsChecked)    
                        {
                            if (communication.isConnected && communication != null)
                            {
                                string inputDataToSend = txtInputData.Text + "\r\n";

                                switch (communication.ConnectType)
                                {
                                    case "Serial":
                                        communication.WriteData(inputDataToSend);
                                        WriteSerialDataLogToFile(inputDataToSend, "Send");
                                        break;
                                    case "Tcp Client":
                                        communication.WriteData(inputDataToSend);
                                        break;
                                    case "Tcp Servers":
                                        communication.WriteData(cbxAddressOfClient.Text, inputDataToSend);
                                        break;
                                    case "UDP":
                                        communication.WriteData(inputDataToSend, txtUdpRemoteHostAdress.Text, Convert.ToInt32(txtRemoteHostPort.Text));
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                        else
                        {
                            if (communication.isConnected && communication != null)
                            {
                                string inputDataToSend = txtInputData.Text;

                                switch (communication.ConnectType)
                                {
                                    case "Serial":
                                        WriteSerialDataLogToFile(inputDataToSend, "Send");
                                        communication.WriteData(inputDataToSend);
                                        break;
                                    case "Tcp Client":
                                        communication.WriteData(inputDataToSend);
                                        break;
                                    case "Tcp Servers":
                                        communication.WriteData(cbxAddressOfClient.Text, inputDataToSend);
                                        break;
                                    case "UDP":
                                        communication.WriteData(inputDataToSend, txtUdpRemoteHostAdress.Text, Convert.ToInt32(txtRemoteHostPort.Text));
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                        break;
                    case "HEX":
                        byte[] bytesToSend = communication.HexStringToByteArray(txtInputData.Text);

                        if (communication != null)
                        {
                            string connectType = communication.ConnectType;

                            if (connectType == "Serial" || connectType == "Tcp Client")
                            {
                                communication.WriteHexData(bytesToSend, 0, bytesToSend.Length);
                            }
                            else if (connectType == "UDP")
                            {
                                communication.WriteHexData(bytesToSend, txtUdpRemoteHostAdress.Text, Convert.ToInt32(txtRemoteHostPort.Text));
                            }
                            else if (connectType == "Tcp Servers")
                            {
                                communication.WriteHexData(cbxAddressOfClient.Text, bytesToSend);
                            }
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

        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
    }
}