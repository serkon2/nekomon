using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using nekoMonitor.Hardware;

public class Program
{
    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        MyApplicationContext app = new MyApplicationContext();
        Application.Run(app);
    }
}

class MyApplicationContext : ApplicationContext
{
    public MyApplicationContext()
    {
        NotifyIcon icon = new NotifyIcon();
        ContextMenu menu = new ContextMenu();
        MenuItem menuItem1 = new MenuItem();
        menu.MenuItems.AddRange(new MenuItem[] { menuItem1 });
        menuItem1.Index = 0;
        menuItem1.Text = "E&xit";
        menuItem1.Click += new EventHandler(delegate(Object o, EventArgs a) {
            Environment.Exit(0);
        });
        icon.Icon = new System.Drawing.Icon(@"Resources/smallicon.ico");
        icon.Visible = true;
        icon.ContextMenu = menu;

        new Thread(worker).Start();
    }

    void worker()
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

            if (request.Url.LocalPath == "/smart")
            {
                var json = new JavaScriptSerializer().Serialize(HDD.GetSMART());
                buffer = System.Text.Encoding.UTF8.GetBytes(json);
            }
            else if (request.Url.LocalPath == "/hardware")
            {
                var json = new JavaScriptSerializer().Serialize(nekoMonitor.Hardware.Monitor.getData());
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