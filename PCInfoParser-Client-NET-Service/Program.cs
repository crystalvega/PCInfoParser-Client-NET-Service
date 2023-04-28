using System.ServiceProcess;

namespace PCInfoParser_Client_NET_Service
{
    internal static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        static void Main()
        {
            string[,,] smart = GetConfiguration.Disk();
            string[,] general = GetConfiguration.General(smart);
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service1()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
