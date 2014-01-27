using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using nekoMonitor.Hardware;

namespace HTTPServer
{
    class Client
    {
        private void SendError(TcpClient Client, int Code)
        {
            string CodeStr = Code.ToString() + " " + ((HttpStatusCode)Code).ToString();
            string Html = "<html><body><h1>" + CodeStr + "</h1></body></html>";
            string Str = "HTTP/1.1 " + CodeStr + "\nContent-type: text/html\nContent-Length:" + Html.Length.ToString() + "\n\n" + Html;
            byte[] Buffer = Encoding.ASCII.GetBytes(Str);
            Client.GetStream().Write(Buffer, 0, Buffer.Length);
            Client.Close();
        }

        public Client(TcpClient Client)
        {
            string Request = "";
            byte[] Buffer = new byte[1024];
            int Count;
            while ((Count = Client.GetStream().Read(Buffer, 0, Buffer.Length)) > 0)
            {
                Request += Encoding.ASCII.GetString(Buffer, 0, Count);
                if (Request.IndexOf("\r\n\r\n") >= 0 || Request.Length > 4096)
                {
                    break;
                }
            }

            Match ReqMatch = Regex.Match(Request, @"^\w+\s+([^\s\?]+)[^\s]*\s+HTTP/.*|");

            if (ReqMatch == Match.Empty)
            {
                SendError(Client, 400);
                return;
            }

            byte[] buffer;

            string RequestUri = ReqMatch.Groups[1].Value;
            RequestUri = Uri.UnescapeDataString(RequestUri);

            if (RequestUri == "/smart")
            {
                var json = new JavaScriptSerializer().Serialize(HDD.GetSMART());
                buffer = System.Text.Encoding.UTF8.GetBytes(json);
            }
            else if (RequestUri == "/hardware")
            {
                var json = new JavaScriptSerializer().Serialize(nekoMonitor.Hardware.Monitor.getData());
                buffer = System.Text.Encoding.UTF8.GetBytes(json);
            }
            else if (RequestUri == "/suspend")
            {
                buffer = System.Text.Encoding.UTF8.GetBytes("OK");
            }
            else
            {
                buffer = System.Text.Encoding.UTF8.GetBytes("Invalid request");
            }

            // Посылаем заголовки
            string Headers = "HTTP/1.1 200 OK\nContent-Type: text/plain\nContent-Length: " + buffer.Length + "\n\n";
            byte[] HeadersBuffer = Encoding.ASCII.GetBytes(Headers);
            Client.GetStream().Write(HeadersBuffer, 0, HeadersBuffer.Length);
            Client.GetStream().Write(buffer, 0, buffer.Length);
            Client.Close();

            if (RequestUri == "/suspend")
            {
                Application.SetSuspendState(PowerState.Suspend, true, true);
            }
        }
    }

    class Server
    {
        TcpListener Listener;

        public Server(int Port)
        {
            Listener = new TcpListener(IPAddress.Any, Port);
            Listener.Start();
            while (true)
            {
                TcpClient Client = Listener.AcceptTcpClient();
                Thread Thread = new Thread(new ParameterizedThreadStart(ClientThread));
                Thread.Start(Client);
            }
        }

        static void ClientThread(Object StateInfo)
        {
            new Client((TcpClient)StateInfo);
        }

        ~Server()
        {
            if (Listener != null)
            {
                Listener.Stop();
            }
        }
    }
}
