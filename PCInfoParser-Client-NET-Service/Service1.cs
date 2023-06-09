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
            while (!_cancel)
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
