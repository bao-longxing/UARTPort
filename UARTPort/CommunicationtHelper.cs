using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using static UARTPort.MainWindow;
using System.Diagnostics;
using System.Windows.Threading;
using System.Threading;
using System.Net.Http;
using System.Net.NetworkInformation;
namespace UARTPort
{
    public abstract class ICommunication
    {

        public bool isConnected = false;
        public string ConnectType = string.Empty;
        public virtual void Connect() { }
        public virtual void Connect(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits) { }
        public virtual void Connect(string serversType, string ipAddress, int port) { }
        public abstract void WriteData(string value);//Serial、Tcp Client
        public virtual void WriteData(string ipAndPort, string value) { }//Tcp Servers
        public virtual void WriteData(string value, string hostname, int port) { }//UDP
        public abstract void WriteHexData(byte[] buffer, int offset, int count);//Serial、Tcp Client HEX
        public virtual void WriteHexData(byte[] bytes, string hostname, int port) { }//UDP HEX
        public virtual void WriteHexData(string ipAndPort, byte[] bytes) { }//TcpServer HEX
        public virtual void SendDataToMainForm(byte[] receiveBytes) { }//NetWork
        public virtual void SetNetWorkCallback(NetWorkConnectCallback function) { }//TCP Callback
        public abstract void Disconnect();
        public string AcsiiToHex(string asciiString)
        {
            StringBuilder hex = new StringBuilder(asciiString.Length * 2);
            foreach (char c in asciiString)
            {
                hex.AppendFormat("{0:X2}", (int)c);
            }
            return hex.ToString();
        }
        public string HexToAcsii(string hexString)
        {
            string ascii = string.Empty;
            for (int i = 0; i < hexString.Length; i += 2)
            {
                string hs = hexString.Substring(i, 2);
                ascii += Convert.ToChar(Convert.ToUInt32(hs, 16));
            }
            return ascii;
        }
        // 将16进制字符串转换为字节数组的方法
        public byte[] HexStringToByteArray(string inPuthexString)
        {
            //去除空格
            string hexString = inPuthexString.Replace(" ", "");
            if (hexString.Length % 2 != 0)
                throw new ArgumentException("16进制字符串的长度必须是偶数。");

            byte[] bytes = new byte[hexString.Length / 2];
            for (int i = 0; i < hexString.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }
            return bytes;
        }
    }
    class NetWorkHelper : ICommunication
    {
        ~NetWorkHelper()
        {
            Disconnect();
        }
        private UdpClient udpClient;
        TcpClient tcpClient;
        TcpListener tcpListener;
        NetworkStream netStream;
        NetWorkReceiveDelegate netWorkReceiveDelegate;
        NetWorkConnectCallback netWorkCallback;
        Dictionary<string, NetworkStream> netConnectedClient = new Dictionary<string, NetworkStream>();
        //接收数据回调
        public NetWorkHelper(NetWorkReceiveDelegate netWorkReceive) { netWorkReceiveDelegate = netWorkReceive; }
        //连接状态回调
        public override void SetNetWorkCallback(NetWorkConnectCallback function)
        {
            netWorkCallback = function;
        }


        //连接
        public override async void Connect(string serversType, string ipAddress, int port)
        {
            try
            {
                switch (serversType)
                {
                    case "Tcp Client":
                        //连接
                        tcpClient = new TcpClient();
                        await tcpClient.ConnectAsync(ipAddress, port);
                        netWorkCallback();
                        isConnected = true;
                        ConnectType = "Tcp Client";
                        TcpClientRecive();
                        break;
                    case "UDP":
                        //连接
                        if (ipAddress == "0.0.0.0")
                        {
                            udpClient = new UdpClient(port);
                            netWorkCallback();
                            isConnected = true;
                            ConnectType = "UDP";
                        }
                        else
                        {
                            IPEndPoint iPEnd = new IPEndPoint(IPAddress.Parse(ipAddress), port);
                            udpClient = new UdpClient(iPEnd);
                            netWorkCallback();
                            isConnected = true;
                            ConnectType = "UDP";
                        }

                        //接收
                        udpClient.BeginReceive(UdpReceiveCallback, udpClient);
                        break;
                    case "Tcp Servers":
                        //连接
                        tcpListener = new TcpListener(IPAddress.Parse(ipAddress), port);
                        tcpListener.Start();
                        netWorkCallback();
                        isConnected = true;
                        ConnectType = "Tcp Servers";
                        //新连接
                        while (isConnected)
                        {
                            tcpClient = await tcpListener.AcceptTcpClientAsync();
                            IPEndPoint remoteEndPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
                            TcpServerRecive(tcpClient);
                        }
                        //接收


                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                if (e.Message == "Unable to read data from the transport connection: 由于线程退出或应用程序请求，已中止 I/O 操作。.")
                {
                    Debug.WriteLine(e.Message);
                    return;
                }
                if (e.Message == "由于线程退出或应用程序请求，已中止 I/O 操作。")
                {
                    Debug.WriteLine(e.Message);
                    return;
                }
                MessageBox.Show(e.Message);
            }
        }
        //断开
        public override void Disconnect()
        {
            if (netStream != null)
            {
                netStream.Close();
            }
            if (udpClient != null)
            {
                udpClient.Close();
            }
            if (tcpClient != null)
            {
                tcpClient.Dispose();
                tcpClient.Close();
            }
            if (tcpListener != null)
            {
                tcpListener.Dispose();
                tcpListener.Stop();
                tcpListener = null;
            }
            isConnected = false;
            ConnectType = string.Empty;
        }

        //Tcp Server 接收
        public async void TcpServerRecive(TcpClient client)
        {
            IPEndPoint remoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;

            NetworkStream stream = client.GetStream();

            netConnectedClient.Add(remoteEndPoint.ToString(), stream);

            byte[] bytsvalue = Encoding.UTF8.GetBytes("\r\nServers: Client Online" + "\r\nDate:  " + DateTime.Now + "\r\nClient:  " + remoteEndPoint.ToString());

            SendDataToMainForm(bytsvalue);

            byte[] buffer = new byte[1024];

            while (true)
            {
                try
                {
                    int numberOfRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                    if (numberOfRead != 0)
                    {
                        byte[] result = new byte[numberOfRead];
                        Array.Copy(buffer, result, numberOfRead);
                        SendDataToMainForm(result);
                    }
                    else
                    {
                        //设备下线
                        bytsvalue = Encoding.UTF8.GetBytes("\r\nServers: Client Offline" + "\r\nDate:  " + DateTime.Now + "\r\nClient:  " + remoteEndPoint.ToString());
                        netConnectedClient.Remove(remoteEndPoint.ToString());
                        SendDataToMainForm(bytsvalue);
                        break;
                    }
                }

                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }


            }
        }
        //Tcp Client 接收
        public async void TcpClientRecive()
        {
            //接收
            if (tcpClient.Connected)
            {
                netStream = tcpClient.GetStream();
            }
            while (isConnected)
            {
                if (!tcpClient.Client.Poll(1, SelectMode.SelectRead))
                {
                    byte[] buffer = new byte[1024];
                    await netStream.ReadAsync(buffer, 0, buffer.Length);
                    SendDataToMainForm(buffer);
                }
                else
                {
                    IPEndPoint remoteEndPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
                    byte[] bytsvalue = Encoding.UTF8.GetBytes("\r\nServers: Client Offline" + "\r\nDate:  " + DateTime.Now + "\r\nClient:  " + remoteEndPoint.ToString());
                    SendDataToMainForm(bytsvalue);
                    break;
                }

            }
        }
        //UDP        接收 
        private void UdpReceiveCallback(IAsyncResult ar)
        {
            try
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] receiveBytes = udpClient.EndReceive(ar, ref endPoint);
                SendDataToMainForm(receiveBytes);
                udpClient.BeginReceive(UdpReceiveCallback, null);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

        }

        //接收并调用窗口类的委托
        public override void SendDataToMainForm(byte[] receiveBytes)
        {
            netWorkReceiveDelegate(receiveBytes);
        }

        //TCP           ASCII发送
        public override void WriteData(string value) 
        {
            byte[] data = Encoding.ASCII.GetBytes(value);
            netStream.Write(data, 0, data.Length);
        }
        //UDP           ASCII发送
        public override void WriteData(string value, string hostname, int port)
        {
            byte[] data = Encoding.ASCII.GetBytes(value);

            udpClient.Send(data, data.Length, hostname, port);
        }
        //TcpServers    ACSII发送
        public override void WriteData(string ipAndPort, string value)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(value);
            netConnectedClient[ipAndPort].Write(buffer, 0, buffer.Length);
        }
        //TcpServers    HEX发送
        public override void WriteHexData(string ipAndPort, byte[] bytes)
        {
            netConnectedClient[ipAndPort].Write(bytes, 0, bytes.Length);
        }
        //UDP           HEX发送
        public override void WriteHexData(byte[] buffer, string hostname, int port)
        {
            udpClient.Send(buffer, buffer.Length, hostname, port);
        }
        //Tcp           HEX发送
        public override void WriteHexData(byte[] buffer, int offset, int count)
        {

            netStream = tcpClient.GetStream();

            if (netStream.CanWrite)
            {
                netStream.Write(buffer, offset, count);
            }
        }
    }
    class SerialPortHelper : ICommunication
    {

        public SerialPort serialPort;
        SerialPortHelper_DataReceived serialCallback;

        public SerialPortHelper(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits, SerialPortHelper_DataReceived serialDelegate)
        {
            serialPort = new SerialPort();
            serialPort.PortName = portName;
            serialPort.BaudRate = baudRate;
            serialPort.Parity = parity;
            serialPort.DataBits = dataBits;
            serialPort.StopBits = stopBits;
            serialCallback = serialDelegate;
        }
        //连接
        public override void Connect()
        {
            try
            {
                serialPort.Open();
                isConnected = true;
                ConnectType = "Serial";
                serialPort.DataReceived += SerialPort_DataReceived;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        //断开
        public override void Disconnect()
        {
            try
            {
                serialPort.Close();
                isConnected = false;
                ConnectType = string.Empty;
                serialPort.DataReceived -= SerialPort_DataReceived;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        //接收
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            serialCallback(sender, e);
        }
        //写入ASCII
        public override void WriteData(string value)
        {
            if (serialPort.IsOpen)
            {
                serialPort.Write(value);
            }

        }
        //写入HEX
        public override void WriteHexData(byte[] buffer, int offset, int count)
        {
            if (serialPort.IsOpen)
            {
                serialPort.Write(buffer, offset, count);
            }
        }
    }
}
