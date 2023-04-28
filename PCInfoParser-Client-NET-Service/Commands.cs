using System;
using System.IO;
using System.Reflection;
using System.ServiceProcess;

namespace PCInfoParser_Client_NET_Service
{
    internal static class Service
    {
        private static bool IsInstalled(string servicename)
        {
            using (ServiceController controller =
                new ServiceController(servicename))
            {
                try
                {
                    ServiceControllerStatus status = controller.Status;
                }
                catch
                {
                    return false;
                }
                return true;
            }
        }

        internal static void Start(string servicename)
        {
            if (!IsInstalled(servicename)) return;

            using (ServiceController controller =
                new ServiceController(servicename))
            {
                try
                {
                    if (controller.Status != ServiceControllerStatus.Running)
                    {
                        controller.Start();
                        controller.WaitForStatus(ServiceControllerStatus.Running,
                            TimeSpan.FromSeconds(10));
                    }
                }
                catch
                {
                    throw;
                }
            }
        }
    }
    internal static class Command
    {
        public static void FileSave(string filename, string[,] arr)
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
        public static void FileSave(string filename, string[,,] arr)
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
        public static void UnpackExe()
        {
            // Получаем путь к системному каталогу (обычно C:\Windows\System32)
            string systemDirectory = Path.Combine("C:\\Windows\\Temp", "DiskInfo");
            string exePath = Path.Combine(systemDirectory, "DiskInfo32.exe");
            string cdiFolder = Path.Combine(systemDirectory, "CdiResource");
            string dialogFolder = Path.Combine(cdiFolder, "dialog");
            string languageFolder = Path.Combine(cdiFolder, "language");
            if (!Directory.Exists(systemDirectory) || !File.Exists(exePath) || !Directory.Exists(cdiFolder) || !Directory.Exists(dialogFolder) || !Directory.Exists(languageFolder))
            {
                Directory.CreateDirectory(systemDirectory);
                Directory.CreateDirectory(cdiFolder);
                Directory.CreateDirectory(dialogFolder);
                Directory.CreateDirectory(languageFolder);
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PCInfoParser_Client_NET_Service.DiskInfo32.exe"))
                {
                    using (FileStream fileStream = new FileStream(exePath, FileMode.Create))
                    {
                        stream.CopyTo(fileStream);
                    }
                }
                // Копируем содержимое папок из внедренных ресурсов в системный каталог
                string graphPath = Path.Combine(dialogFolder, "Graph.html");
                string englishPath = Path.Combine(languageFolder, "English.lang");
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PCInfoParser_Client_NET_Service.CdiResource.dialog.Graph.html"))
                {
                    using (FileStream fileStream = new FileStream(graphPath, FileMode.Create))
                    {
                        stream.CopyTo(fileStream);
                    }
                }
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PCInfoParser_Client_NET_Service.CdiResource.language.English.lang"))
                {
                    using (FileStream fileStream = new FileStream(englishPath, FileMode.Create))
                    {
                        stream.CopyTo(fileStream);
                    }
                }
            }
        }
    }
}
