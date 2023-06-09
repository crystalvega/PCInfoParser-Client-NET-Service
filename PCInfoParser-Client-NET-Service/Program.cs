using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;

namespace PCInfoParser_Client_NET_Service
{
    public static class Dir
    {
        public static string Get(string filename)
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            path = Path.GetDirectoryName(path);
            string Directory = Path.Combine(path, filename);
            return Directory;
        }
    }



    internal static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        /// 

        static void Main(string[] args)
        {
            IniFile ini = new(Dir.Get("PCInfoParser-Client.ini"));
            Configuration configuration = new();
            Command.UnpackExe();
            DateTime date1;
            string[] lastSend;
            bool firstsend = false;
            string checkdays = ini.GetValue("App", "Autosend");
            string lastSendDay = ini.GetValue("App", "LastSend");
            if (lastSendDay == null)
            {
                lastSend = new string[3] { "11", "11", "1111" };
                firstsend = true;
            }
            else lastSend = lastSendDay.Split('.');
            date1 = new DateTime(Convert.ToInt32(lastSend[2]), Convert.ToInt32(lastSend[1]), Convert.ToInt32(lastSend[0]));
            while (true)
            {
                DateTime date2 = DateTime.Today;
                TimeSpan difference = date2.Subtract(date1);

                if (difference.TotalDays > Convert.ToInt32(checkdays) || firstsend)
                {
                    configuration.Generate();
                    string[,,] smart = configuration.SmartGet();
                    string[,] general = configuration.GeneralGet();
                    string lan = configuration.Lan();

                    Connection client = new(ini, general, smart, lan);
                    client.Send();
                    string todaynew = client.TodayGet();
                    lastSend = todaynew.Split('.');
                    date1 = new DateTime(Convert.ToInt32(lastSend[2]), Convert.ToInt32(lastSend[1]), Convert.ToInt32(lastSend[0]));
                    ini.SetValue("App", "LastSend", todaynew);
                    ini.Save();
                    Command.FileSave("Smart.txt", smart);
                    Command.FileSave("General.txt", general);
                }
                Thread.Sleep(3600000);
            }


            if (Environment.UserInteractive)
            {
                if (args != null && args.Length > 0)
                {
                    switch (args[0])
                    {
                        case "--install":
                            try
                            {
                                var appPath = Assembly.GetExecutingAssembly().Location;
                                System.Configuration.Install.ManagedInstallerClass.InstallHelper(new string[] { appPath });
                                if (File.Exists("PCInfoParser-Client.ini")) Service.Start("PCInfoParcer");
                                else Process.Start("PCInfoParser-Client-NET-Service-Configurator.exe");

                            }
                            catch (Exception ex) { Console.WriteLine(ex.Message); }
                            break;
                        case "--uninstall":
                            try
                            {
                                var appPath = Assembly.GetExecutingAssembly().Location;
                                System.Configuration.Install.ManagedInstallerClass.InstallHelper(new string[] { "/u", appPath });
                            }
                            catch (Exception ex) { Console.WriteLine(ex.Message); }
                            break;
                    }
                }
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                new Service1()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
