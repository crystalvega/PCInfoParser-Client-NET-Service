using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace PCInfoParser_Client_NET_Service
{
    internal static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        static void Main()
        {
            string pnpDeviceId = "";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT PNPDeviceID FROM Win32_DesktopMonitor");
            ManagementObjectCollection monitors = searcher.Get();
            ManagementObject firstMonitor = null;
            foreach (ManagementObject monitor in monitors)
            {
                firstMonitor = monitor;
                break;
            }
            if (firstMonitor != null)
            {
                pnpDeviceId = (string)firstMonitor["PNPDeviceID"];
                Console.WriteLine(pnpDeviceId);
            }
            object[] result = GetDisplay.Get(pnpDeviceId);
            Console.WriteLine(result[0]);
            Console.WriteLine(result[1]);
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service1()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
