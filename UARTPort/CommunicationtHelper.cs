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
namespace UARTPort
{

    public abstract class ICommunication
    {
        public bool isConnected = false;
        public string ConnectType = string.Empty;
        public abstract void Disconnect();
        public abstract void WriteData(string value);
        public abstract void WriteHexData(byte[] buffer, int offset, int count);
        public virtual void Receive(byte[] receiveBytes) { }//NetWork
        public virtual void Connect() { }

        public virtual void Connect(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits) { }
        public virtual async void Connect(string serversType, string ipAddress, int port) { }
    
        public virtual void SetNetWorkCallback(NetWorkConnectCallback function) { }//TCP Callback
        public virtual void WriteData(string value, string hostname, int port) { }//UDP
        public virtual void WriteHexData(byte[] bytes, string hostname, int port) { }//UDP
        

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
        private UdpClient udpClient;
        TcpClient tcpClient;
        TcpListener tcpListener;
        NetworkStream NetStream;
        NetWorkReceiveDelegate netWorkReceiveDelegate;
        NetWorkConnectCallback netWorkCallback;


        public NetWorkHelper(NetWorkReceiveDelegate netWorkReceive) { netWorkReceiveDelegate = netWorkReceive; }

        public override void SetNetWorkCallback(NetWorkConnectCallback function) 
        {
            netWorkCallback = function;
        }

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

                        //接收
                        if (tcpClient.Connected)
                        {
                            NetStream = tcpClient.GetStream();
                        }
                        while (isConnected)
                        {
                            byte[] buffer = new byte[1024];
                            await NetStream.ReadAsync(buffer, 0, buffer.Length);
                            Receive(buffer);
                        }

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
                        tcpListener = new TcpListener(IPAddress.Parse(ipAddress),port);
                        tcpListener.Start();
                        netWorkCallback();
                        isConnected = true;
                        ConnectType = "Tcp Servers";
                        //连接
                        while (isConnected) 
                        {
                            tcpClient = await tcpListener.AcceptTcpClientAsync();
                            IPEndPoint remoteEndPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
                            byte[] bytsvalue = Encoding.UTF8.GetBytes("\r\nDate:  " + DateTime.Now + "\r\nUser:  " + remoteEndPoint.ToString() + "   isOnline");
                            Receive(bytsvalue);
                            await TcpServerRecive(tcpClient);
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
                MessageBox.Show(e.Message);
            }
        }
        
        //Tcp Server接收
        public async Task TcpServerRecive(TcpClient client) 
        {
            byte[] buffer = new byte[1024];

            NetworkStream stream = client.GetStream();

            while (true) 
            {
                if (!stream.CanRead)
                {
                    int bytesReadLength = await stream.ReadAsync(buffer, 0, buffer.Length);
                    byte[] result = new byte[bytesReadLength];
                    Array.Copy(buffer, result, bytesReadLength);
                    Receive(result);
                    //设备下线通知
                }
                else
                {
                    break;
                }

            }
        }

        //UDP接收回调
        private void UdpReceiveCallback(IAsyncResult ar)
        {
            try
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] receiveBytes = udpClient.EndReceive(ar, ref endPoint);
                Receive(receiveBytes); 
                udpClient.BeginReceive(UdpReceiveCallback, null);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

        }
        public override void Receive(byte[] receiveBytes)
        {
            netWorkReceiveDelegate(receiveBytes);
        }
        ~NetWorkHelper() 
        {
            Disconnect();
        }

        
  
        public override void Disconnect()
        {
            if (NetStream!=null)
            {
                NetStream.Close();
            }
            if (udpClient!=null)
            {
                udpClient.Close();
            }
            if (tcpClient!=null)
            {
                tcpClient.Dispose();
                tcpClient.Close();
            }
            isConnected = false;
            ConnectType = string.Empty;
        }

        public override void WriteData(string value) //TCP
        {
            byte[] data = Encoding.ASCII.GetBytes(value);

            switch (ConnectType)
            {
                case "Tcp Client":
                    if (NetStream.CanWrite)
                    {
                        NetStream.Write(data, 0, data.Length);
                    }
                    break;
                default:
                    break;
            }
            
        }
        public override void WriteData(string value,string hostname,int port) //UDP
        {
            byte[] data = Encoding.ASCII.GetBytes(value);

            udpClient.Send(data, data.Length, hostname, port);
        }
        public override void WriteHexData(byte[] buffer, string hostname, int port)
        {
            udpClient.Send(buffer, buffer.Length, hostname, port);
        }
        public override void WriteHexData(byte[] buffer, int offset, int count)
        {
            
            NetStream = tcpClient.GetStream();

            if (NetStream.CanWrite)
            {
                NetStream.Write(buffer, offset, count);
            }
        }
        
    }
    class SerialPortHelper : ICommunication
    {

        public SerialPort serialPort;
        SerialPortHelper_DataReceived serialCallback;

        public SerialPortHelper(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits ,SerialPortHelper_DataReceived serialDelegate) 
        {
            serialPort = new SerialPort();
            serialPort.PortName = portName;
            serialPort.BaudRate = baudRate;
            serialPort.Parity = parity;
            serialPort.DataBits = dataBits;
            serialPort.StopBits = stopBits;
            serialCallback = serialDelegate;
        }
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

        public override void Disconnect()
        {
            try
            {
                serialPort.Close();
                isConnected = false;
                ConnectType=string.Empty;
                serialPort.DataReceived -= SerialPort_DataReceived;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            serialCallback(sender,e);
        }

        public  override void WriteData(string value) 
        {
            if (serialPort.IsOpen)
            {
                serialPort.Write(value);
            }

        }
        public override void WriteHexData(byte[] buffer, int offset, int count)
        {
            if (serialPort.IsOpen)
            {
                serialPort.Write(buffer, offset, count);
            }
        }
        

        

    }
}
