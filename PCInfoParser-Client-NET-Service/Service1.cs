using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace PCInfoParser_Client_NET_Service
{
    public partial class Service1 : ServiceBase
    {
        private bool _cancel;
            public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _cancel = false;
            Task.Run(() => Processing());

            EventLog.WriteEntry("We did it! Started", EventLogEntryType.Information);

        }
        private void Processing()
        {
            try
            {
                Command.UnpackExe();
                string[,,] smart = GetConfiguration.Disk();
                string[,] general = GetConfiguration.General(smart);
                Command.FileSave("Smart.txt", smart);
                Command.FileSave("General.txt", general);
                while (!_cancel)
                {
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        protected override void OnStop()
        {
            _cancel = true;
            Command.FileRemove("Smart.txt");
            Command.FileRemove("General.txt");
            EventLog.WriteEntry("We did it! Stoped", EventLogEntryType.Information);
        }
    }
}
