using System;
using System.Reflection;
using System.ServiceProcess;
using System.IO;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Linq;

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

        static void Main(string[] args)
        {
            IniFile ini = new(Dir.Get("PCInfoParser-Client.ini"));
            string checkdays = ini.GetValue("App", "Autosend");
            Command.UnpackExe();
            string lan = GetConfiguration.Lan();
            string[,,] smart = GetConfiguration.Disk();

            //string arrayStringSMART = "string[,,] smart = {";
            //for (int i = 0; i < smart.GetLength(0); i++)
            //{
            //    arrayStringSMART += "{";
            //    for (int j = 0; j < smart.GetLength(1); j++)
            //    {
            //        arrayStringSMART += "{ ";
            //        for (int k = 0; k < smart.GetLength(2); k++)
            //        {
            //            arrayStringSMART += $"\"{smart[i, j, k]}\", ";
            //        }
            //        arrayStringSMART = arrayStringSMART.TrimEnd(',', ' ') + " }, ";
            //    }
            //    arrayStringSMART = arrayStringSMART.TrimEnd(',', ' ') + "}, ";
            //}
            //arrayStringSMART = arrayStringSMART.TrimEnd(',', ' ') + "};";

            string[,] general = GetConfiguration.General(smart);

            //string arrayStringGEN = "string[,] general = {";
            //for (int i = 0; i < general.GetLength(0); i++)
            //{
            //    arrayStringGEN += "{ ";
            //    for (int j = 0; j < general.GetLength(1); j++)
            //    {
            //        arrayStringGEN += $"\"{general[i, j]}\", ";
            //    }
            //    arrayStringGEN = arrayStringGEN.TrimEnd(',', ' ') + " }, ";
            //}
            //arrayStringGEN = arrayStringGEN.TrimEnd(',', ' ') + "};";

            Connection client = new Connection(ini, general, smart, lan);
            client.Send();
            Command.FileSave("Smart.txt", smart);
            Command.FileSave("General.txt", general);
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
