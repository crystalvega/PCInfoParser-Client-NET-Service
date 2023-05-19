using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

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
            if (File.Exists("PCInfoParser-Client.ini"))
            {
                Task.Run(() => Processing());
                EventLog.WriteEntry("We did it! Started", EventLogEntryType.Information);
            }
            else
            {
                try
                { 
                Process.Start("PCInfoParser-DB-Viewer-NET.exe");
                }
                catch (Exception) 
                {
                    OnStop();
                }
            }
            }
        private void Processing()
        {
            try
            {
                if (File.Exists("PCInfoParser-Client.ini"))
                {
                    IniFile ini = new(Dir.Get("PCInfoParser-Client.ini"));
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
                    DateTime date2 = DateTime.Today;

                    while (!_cancel)
                    {
                        TimeSpan difference = date2.Subtract(date1);

                        if (difference.TotalDays > Convert.ToInt32(checkdays) || firstsend)
                        {
                            Command.UnpackExe();
                            string lan = GetConfiguration.Lan();
                            string[,,] smart = GetConfiguration.Disk();

                            string[,] general = GetConfiguration.General(smart);

                            Connection client = new(ini, general, smart, lan);
                            client.Send();
                            string todaynew = client.todayget();
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
                else Process.Start("PCInfoParser-DB-Viewer-NET.exe");
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
