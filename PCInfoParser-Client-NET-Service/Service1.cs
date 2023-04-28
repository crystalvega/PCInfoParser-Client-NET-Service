using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace PCInfoParser_Client_NET_Service
{
    public static class File
    {
        public static void Save(string filename, string[,] arr)
        {
            StreamWriter writer = new StreamWriter(filename);

            // Записываем размеры массива в первую строку файла
            writer.WriteLine(arr.GetLength(0));
            writer.WriteLine(arr.GetLength(1));

            // Записываем элементы массива в следующие строки файла
            for (int i = 0; i < arr.GetLength(0); i++)
            {
                for (int j = 0; j < arr.GetLength(1); j++)
                {
                    writer.Write(arr[i, j] + " ");
                }
                writer.WriteLine();
            }

            // Закрываем файл
            writer.Close();
        }
        public static void Save(string filename, string[,,] arr)
        {
            StreamWriter writer = new StreamWriter(filename);

            // Записываем размеры массива в первую строку файла
            writer.WriteLine(arr.GetLength(0));
            writer.WriteLine(arr.GetLength(1));
            writer.WriteLine(arr.GetLength(2));

            // Записываем элементы массива в следующие строки файла
            for (int i = 0; i < arr.GetLength(0); i++)
            {
                for (int j = 0; j < arr.GetLength(1); j++)
                {
                    for (int k = 0; k < arr.GetLength(2); k++)
                    {
                        writer.Write(arr[i, j, k] + " ");
                    }
                    writer.WriteLine();
                }
                writer.WriteLine();
            }

            // Закрываем файл
            writer.Close();
        }
    }
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
            string[,,] smart = GetConfiguration.Disk();
            string[,] general = GetConfiguration.General(smart);
            File.Save("C:\\Windows\\Temp\\Smart.txt", smart);
            File.Save("C:\\Windows\\Temp\\General.txt", general);
            Task.Run(() => Processing());

            EventLog.WriteEntry("We did it! Started", EventLogEntryType.Information);

        }
        private void Processing()
        {
            try
            {
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

            EventLog.WriteEntry("We did it! Stoped", EventLogEntryType.Information);
        }
    }
}
