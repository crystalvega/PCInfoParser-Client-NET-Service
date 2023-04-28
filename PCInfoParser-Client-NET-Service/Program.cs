using System;
using System.Reflection;
using System.ServiceProcess;

namespace PCInfoParser_Client_NET_Service
{
    internal static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>

        static void Main(string[] args)
        {
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
                                Service.Start("PCInfoParcer");
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
