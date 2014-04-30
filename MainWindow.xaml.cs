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
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Threading;

namespace RawSocketHttpRequest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private Socket socket;

        private void m_SendHttpPost_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Uri uri = new Uri(m_URL.Text);
                String host = uri.Host;

                Encoding codec = null;
                try
                {
                    codec = Encoding.GetEncoding(m_Encoding.Text);
                }
                catch (Exception)
                {
                    codec = Encoding.UTF8;
                }

                if (null != socket)
                {
                    if (Dns.GetHostAddresses(host)[0] != ((IPEndPoint)socket.RemoteEndPoint).Address)
                    {
                        PrintLog("Mismatched host name");
                        socket.Shutdown(SocketShutdown.Both);

                        if (socket.Connected)
                        {
                            socket.Disconnect(false);
                            socket.Close();
                        }

                        socket.Dispose();
                    }
                }

                socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                PrintLog("Try to connect");

                socket.Connect(host, 80);

                PrintLog("Connected");

                Thread receive = new Thread(() =>
                {
                    byte[] buffer = new byte[1024];
                    int byteCount;

                    try
                    {
                        do
                        {
                            byteCount = socket.Receive(buffer, 1024, (SocketFlags)0);
                            InvokeOnDispatcher(m_ReceiveContent, () => m_ReceiveContent.Text += codec.GetString(buffer, 0, byteCount));
                            InvokeOnDispatcher(m_Message, () => PrintLog("{0} bytes received", byteCount));
                        } while (byteCount > 0);
                    }
                    catch (SocketException ee)
                    {
                        InvokeOnDispatcher(m_Message, () => PrintLog(ee.Message));
                    }
                });

                receive.Start();

                String post = CombinePost(m_PostHead.Text, m_PostContent.Text);

                m_PostedMessage.Text = post;

                socket.Send(Encoding.UTF8.GetBytes(post));

                PrintLog("Message Sent");

                m_ReceiveContent.Text = "";
            }
            catch (UriFormatException ufe)
            {
                m_Message.Text = ufe.Message;
            }
        }

        protected void InvokeOnDispatcher(Control element, Action action)
        {
            element.Dispatcher.Invoke(action);
        }

        private void m_PostContent_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void PrintLog(String content)
        {
            m_Message.Text += String.Format("{0} {1}{2}", DateTime.Now.ToLongTimeString(), content, Environment.NewLine);
        }

        private void PrintLog(String format, params Object[] content)
        {
            PrintLog(String.Format(format, content));
        }

        private String CombinePost(String header, String content)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(header);
            int length = Encoding.UTF8.GetByteCount(content);
            sb.AppendFormat("Content-Length:{0}", length);

            PrintLog("Content-Length:{0}", length);

            sb.AppendLine();
            sb.AppendLine();
            sb.Append(content);

            return sb.ToString();
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != socket)
            {
                socket.Shutdown(SocketShutdown.Both);

                if (socket.Connected)
                {
                    socket.Disconnect(false);
                    socket.Close();
                }

                socket.Dispose();
            }
        }

        private void m_Message_TextChanged(object sender, TextChangedEventArgs e)
        {
            m_Message.ScrollToEnd();
        }
    }
}
