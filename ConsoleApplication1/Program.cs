using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using nekoMonitor;

public class Program
{
    private static Hardware hardware = new Hardware();

    [STAThread]
    public static void Main()
    {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8080/");
        listener.Start();
        Console.WriteLine("Listening...");
        while (true)
        {
            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            Console.WriteLine("REQUEST: " + request.Url.LocalPath);

            byte[] buffer;

            if(request.Url.LocalPath == "/smart")
            {
                var json = new JavaScriptSerializer().Serialize(HDD.GetSMART());
                buffer = System.Text.Encoding.UTF8.GetBytes(json);
            }
            else if (request.Url.LocalPath == "/hardware")
            {
                var json = new JavaScriptSerializer().Serialize(hardware.getUpdatedData());
                buffer = System.Text.Encoding.UTF8.GetBytes(json);
            }
            else if (request.Url.LocalPath == "/suspend")
            {
                buffer = System.Text.Encoding.UTF8.GetBytes("OK");
            }
            else
            {
                buffer = System.Text.Encoding.UTF8.GetBytes("Invalid request");
            }

            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();

            if (request.Url.LocalPath == "/suspend")
            {
                Application.SetSuspendState(PowerState.Suspend, true, true);
            }
        }
    }
}