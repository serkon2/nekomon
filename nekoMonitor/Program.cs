using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Configuration;
using nekoMonitor;
using System.Security.Principal;
using System.Diagnostics;

public class Program
{
    public static PersistentSettings settings = new PersistentSettings();
    public const string appName = "nekoMonitor";

    [STAThread]
    public static void Main()
    {
        if (!IsAdministrator())
        {
            // Restart and run as admin
            ProcessStartInfo startInfo = new ProcessStartInfo(Application.ExecutablePath);
            startInfo.Verb = "runas";
            Process.Start(startInfo);
            Application.Exit();
            return;
        }

        Directory.SetCurrentDirectory(Application.StartupPath);

        settings.Load(Path.ChangeExtension(Application.ExecutablePath, ".config"));        
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        MyApplicationContext app = new MyApplicationContext(settings);
        Application.Run(app);
        Environment.Exit(0);
    }

    public static bool IsInStartup()
    {
        try
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            string v = key.GetValue(appName).ToString();
            if (v != Application.ExecutablePath)
                return false;
            else
                return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    public static void AddStartup()
    {
        Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        key.SetValue(appName, Application.ExecutablePath);
    }
    public static void RemoveStartup()
    {
        Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        key.DeleteValue(appName);
    }
    public static bool IsAdministrator()
    {
        WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}

class MyApplicationContext : ApplicationContext
{
    protected PersistentSettings settings;
    private NotifyIcon icon;

    public MyApplicationContext(PersistentSettings settings)
    {
        icon = new NotifyIcon();
        ContextMenu menu = new ContextMenu();
        MenuItem menuItem1 = new MenuItem();
        MenuItem autoStart = new MenuItem();
        
        menu.MenuItems.AddRange(new MenuItem[] { autoStart, menuItem1 });
        
        autoStart.Index = 0;
        autoStart.Text = "Start at boot";
        autoStart.Checked = Program.IsInStartup();
        
        autoStart.Click += new EventHandler(delegate (Object o, EventArgs a)
        {
            if (autoStart.Checked)
            {
                Program.RemoveStartup();
            } else
            {
                Program.AddStartup();
            }

            autoStart.Checked = Program.IsInStartup();
        });

        menuItem1.Index = 1;
        menuItem1.Text = "E&xit";
        menuItem1.Click += new EventHandler(delegate(Object o, EventArgs a) {
            Application.Exit();
        });

        icon.Icon = new System.Drawing.Icon(@"Resources/smallicon.ico");
        icon.Visible = true;
        icon.ContextMenu = menu;

        this.settings = settings;

        new Thread(worker).Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            icon.Dispose();
        }
        base.Dispose(disposing);
    }

    void worker()
    {
        var Port = Convert.ToInt32(this.settings.GetValue("port", 5025));

        try
        {
            new HTTPServer.Server(Port);
        }
        catch (Exception e)
        {
            MessageBox.Show($"Could not run server on port {Port}: {e.Message}", Program.appName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Exit();
        }
    }
}