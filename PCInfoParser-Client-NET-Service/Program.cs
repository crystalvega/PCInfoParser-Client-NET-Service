using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;

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
            string[,,] smart = GetConfiguration.Disk();

            string[,] general = GetConfiguration.General(smart);
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
