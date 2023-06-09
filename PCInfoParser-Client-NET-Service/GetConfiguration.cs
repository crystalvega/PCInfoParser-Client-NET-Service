using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using EDIDParser;
using Hardware.Info;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;

namespace PCInfoParser_Client_NET_Service
{

    public class Configuration
    {
        private IDictionary<string, string> general = new Dictionary<string, string>();
        private IDictionary<string, string>[] smart = new Dictionary<string, string>[4];

        public Configuration() 
        {
            Generate();
        }

        public string[,] GeneralGet()
        {
            // Получение количества элементов в словаре
            int dictionaryLength = general.Count;

            // Создание двумерного массива с размером [количество элементов, 2]
            string[,] array2D = new string[dictionaryLength, 2];

            // Индекс для итерации по массиву
            int index = 0;

            // Копирование данных из словаря в массив
            foreach (KeyValuePair<string, string> entry in general)
            {
                array2D[index, 0] = entry.Key;
                array2D[index, 1] = entry.Value;
                index++;
            }

            return array2D;
        }

        public string[,,] SmartGet()
        {
            // Определение количества словарей, у которых все значения null
            int nullDictionaryCount = smart.Count(dictionary => dictionary.Values.All(value => value == null));

            // Определение размера массива
            int arrayLength = smart.Length - nullDictionaryCount;
            int maxDictionarySize = smart.Where(dictionary => !dictionary.Values.All(value => value == null)).Max(dictionary => dictionary.Count);

            // Создание трехмерного массива с размером [количество словарей, максимальный размер словаря, 2]
            string[,,] array3D = new string[arrayLength, maxDictionarySize, 2];

            // Индекс для итерации по массиву словарей
            int arrayIndex = 0;

            // Копирование данных из массива словарей в трехмерный массив
            foreach (var dictionary in smart)
            {
                if (dictionary.Values.All(value => value == null))
                {
                    continue; // Пропуск словарей, у которых все значения null
                }

                int index = 0;
                foreach (KeyValuePair<string, string> entry in dictionary)
                {
                    array3D[arrayIndex, index, 0] = entry.Key ?? "";
                    array3D[arrayIndex, index, 1] = entry.Value ?? "";
                    index++;
                }
                arrayIndex++;
            }

            return array3D;
        }

        public void Generate()
        {
            SmartGenerate();
            GeneralGenerate();
        }

        public string Lan()
        {
            return Get.Lan();
        }
        private void GeneralGenerate()
        {
            string[] display = Get.Display();
            string[] printer = Get.Printer();
            string typepc = Get.PCType();
            string motherboard = Get.Motherboard();
            string[] cpu = Get.CPU();
            string temperature = Get.CPUTemperature();
            string[] upgrade = Get.CPUUpgrade(cpu[0]);
            string os = Get.OS();
            string[] ram = Get.RAM();
            string antivirus = Get.Antivirus();

            general["Монитор"] = display[0];
            general["Диагональ"] = display[1];
            general["Тип принтера"] = printer[1];
            general["Модель принтера"] = printer[0];
            general["ПК"] = typepc;
            general["Материнская плата"] = motherboard;
            general["Процессор"] = upgrade[0];
            general["Частота процессора"] = cpu[1];
            general["Баллы Passmark"] = upgrade[1];
            general["Дата выпуска"] = upgrade[2];
            general["Температура процессора"] = temperature;
            general["Тип ОЗУ"] = upgrade[4];
            general["ОЗУ, 1 Планка"] = ram[0];
            general[ "ОЗУ, 2 Планка"] = ram[1];
            general["ОЗУ, 3 Планка"] = ram[2];
            general["ОЗУ, 4 Планка"] = ram[3];
            general["Сокет"] = upgrade[3];
            general["Диск 1"] = smart[0].TryGetValue("Наименование", out string result) ? result : "";
            general["Состояние диска 1"] = smart[0].TryGetValue("Состояние", out result) ? result : "";
            general["Диск 2"] = smart[1].TryGetValue("Наименование", out result) ? result : "";
            general["Состояние диска 2"] = smart[1].TryGetValue("Состояние", out result) ? result : "";
            general["Диск 3"] = smart[2].TryGetValue("Наименование", out result) ? result : "";
            general["Состояние диска 3"] = smart[2].TryGetValue("Состояние", out result) ? result : "";
            general["Диск 4"] = smart[3].TryGetValue("Наименование", out result) ? result : "";
            general["Состояние диска 4"] = smart[3].TryGetValue("Состояние", out result) ? result : "";
            general["Операционная система"] = os;
            general["Антивирус"] = antivirus;
            general[ "CPU Под замену"] = upgrade[5];
            general["Все CPU под сокет"] = upgrade[6];
        }
        private void SmartGenerate()
        {
            string[,] values = new string[7, 2] { { "Model", "Наименование" }, { "Firmware", "Прошивка" }, { "Disk Size", "Размер" }, { "Power On Hours", "Время работы" }, { "Power On Count", "Включён" }, { "Temperature", "Температура" }, { "Health Status", "Состояние" } };

            string directory = Path.Combine(Command.AssemblyDirectory(), "DiskInfo");
            string exePath = Path.Combine(directory, "DiskInfo32.exe");
            string arguments = "/copyexit";
            Process.Start(exePath, arguments).WaitForExit();

            for (int i = 0; i < 4; i++)
                smart[i] = new Dictionary<string, string>();

            
            string[] lines = File.ReadAllLines(Path.Combine(directory, "diskinfo.txt"));
            int i3 = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(" (0"))
                {
                    while (lines[i] != "")
                    {
                        for (int i2 = 0; i2 < 7; i2++)
                            if (lines[i].Contains(values[i2, 0]) && lines[i].Contains(" : "))
                            {
                                if (smart[i3].ContainsKey(values[i2, 1]))
                                    i3++;
                                if (i3 == 4) break;
                                smart[i3][values[i2, 1]] = lines[i].Split(':')[1].Trim();
                            }
                        i++;
                        if (i3 == 4) break;
                    }
                }
                if (i3 == 4) break;
            }
        }
        public static class Get
        {
            static readonly IHardwareInfo hardwareInfo = new HardwareInfo();
            private static readonly RegistryKey registry = Registry.LocalMachine;
            private static byte[] OpenRegistryA(string dir)
            {
                byte[] key = null;
                try
                {
                    RegistryKey rawKeyA = registry.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum\\" + dir + "\\Device Parameters");
                    string name = "EDID";
                    byte[] value = (byte[])rawKeyA.GetValue(name);
                    key = value;
                }
                catch (Exception)
                {
                    // handle exception
                }
                return key;
            }
            private static string Directory()
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
                }
                return pnpDeviceId;
            }
            public static string[] Display()
            {
                string dir = Directory();
                byte[] edid_hex = OpenRegistryA(dir);
                string monitorname = "";
                string[] edid_result = new string[] { "Не найдено", "Не найдено" };
                try
                {
                    EDID edid = new EDID(edid_hex);
                    foreach (var desc in edid.Descriptors)
                    {
                        if (desc.ToString().Contains("MonitorName"))
                        {
                            monitorname = desc.ToString().Split(':')[1].TrimEnd(')');
                            break;
                        }
                    }
                    edid_result[0] = edid.ManufacturerCode + " " + monitorname;
                    edid_result[1] = edid.DisplayParameters.DisplaySizeInInch.ToString();
                }
                catch (Exception) { }
                return edid_result;
            }
            public static string[] Printer()
            {
                PrinterSettings settings = new PrinterSettings();
                string printerName = settings.PrinterName;
                string query = string.Format("SELECT * from Win32_Printer WHERE Name LIKE '%{0}'", printerName);
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
                string[] returnvalue = new string[2] { printerName, "" };

                foreach (ManagementObject printer in searcher.Get())
                {
                    UInt16 capability = 0;
                    UInt16[] capabilities = (UInt16[])printer["Capabilities"];
                    if (capabilities.Length > 0)
                    {
                        capability = capabilities[0];
                        // теперь вы можете использовать значение "capability" вместо "capabilities" в вашем коде
                    }

                    if ((capability & 4) != 0) // PRINTER_CAPABILITY_COPIER
                    {
                        returnvalue[1] = "Копир";
                    }
                    else if ((capability & 8) != 0) // PRINTER_CAPABILITY_FAX
                    {
                        returnvalue[1] = "Факс";
                    }
                    else if ((capability & 256) != 0) // PRINTER_CAPABILITY_MULTI_FUNCTION
                    {
                        returnvalue[1] = "МФУ";
                    }
                    else if ((capability & 2) != 0) // PRINTER_CAPABILITY_PRINTER
                    {
                        returnvalue[1] = "Принтер";
                    }
                    else // PRINTER_CAPABILITY_UNKNOWN
                    {
                        returnvalue[1] = "Неизвестен";
                    }
                }
                return returnvalue;
            }
            public static string PCType()
            {
                string returnvalue = "";
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SystemInformation");

                if (key != null)
                {
                    string value = key.GetValue("ValueName")?.ToString() ?? "";

                    if (value.Contains("Laptop") || value.Contains("Notebook"))
                    {
                        returnvalue = "Ноутбук";
                    }
                    else if (value.Contains("All-in-One"))
                    {
                        returnvalue = "Моноблок";
                    }
                    else
                    {
                        returnvalue = "ПК";
                    }
                }
                else
                {
                    Console.WriteLine("Не удалось определить");
                }
                return returnvalue;
            }
            public static string Motherboard()
            {
                hardwareInfo.RefreshMotherboardList();
                foreach (var motherboard in hardwareInfo.MotherboardList)
                {
                    return motherboard.Manufacturer + " " + motherboard.Product;
                }
                return null;
            }
            public static string[] CPU()
            {
                string[] returnvalue = new string[2] { "", "" };
                hardwareInfo.RefreshCPUList();
                string cpu = hardwareInfo.CpuList[0].Name;
                returnvalue[0] = cpu.Trim();
                returnvalue[1] = hardwareInfo.CpuList[0].MaxClockSpeed.ToString();

                return returnvalue;
            }

            public static string CPUTemperature()
            {
                string temperature = "Не найдено";

                ManagementObjectSearcher searcher = new("root\\CIMV2", "SELECT * FROM Win32_PerfFormattedData_Counters_ThermalZoneInformation");
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    string name = queryObj["Name"].ToString();
                    double temperature_temp = Convert.ToDouble(queryObj["Temperature"]) / 10.0;
                    temperature = temperature_temp.ToString();
                }

                return temperature;
            }

            private static List<string[]> CPULoad()
            {
                List<string[]> data = new();
                Assembly assembly = Assembly.GetExecutingAssembly();
                Stream stream = assembly.GetManifestResourceStream("PCInfoParser_Client_NET_Service.db.xlsx");
                using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Open(stream, false))
                {
                    WorkbookPart workbookPart = spreadsheetDocument.WorkbookPart;
                    IEnumerable<Sheet> sheets = workbookPart.Workbook.Descendants<Sheet>();

                    foreach (Sheet sheet in sheets)
                    {
                        WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
                        Worksheet worksheet = worksheetPart.Worksheet;
                        IEnumerable<Row> rows = worksheet.Descendants<Row>();

                        foreach (Row row in rows)
                        {
                            List<string> rowData = new List<string>();

                            foreach (Cell cell in row.Descendants<Cell>())
                            {
                                string cellValue = string.Empty;

                                if (cell.CellValue != null)
                                {
                                    cellValue = cell.CellValue.InnerText;
                                    if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
                                    {
                                        int index = int.Parse(cellValue);
                                        cellValue = workbookPart.SharedStringTablePart.SharedStringTable.ElementAt(index).InnerText;

                                    }
                                }

                                rowData.Add(cellValue);
                            }

                            data.Add(rowData.ToArray());
                        }
                    }
                }

                return data;
            }
            public static string[] CPUUpgrade(string cpu)
            {
                if (cpu.Contains("(R)")) cpu = cpu.Replace("(R)", "");
                if (cpu.Contains("(C)")) cpu = cpu.Replace("(C)", "");
                if (cpu.Contains("(TM)")) cpu = cpu.Replace("(TM)", "");
                List<string[]> table = CPULoad();
                List<string> CPUS = new();
                string[] stringData = new string[5];
                foreach (string[] tableData in table)
                {
                    if (cpu.Contains(tableData[0]))
                    {
                        stringData = tableData;
                        break;
                    }
                }
                List<string> listData = new(stringData);
                foreach (string[] tableData in table)
                {
                    if (tableData[3] == stringData[3] && tableData[4] == stringData[4])
                    {
                        CPUS.Add(tableData[0]);
                    }
                }
                listData.Add(CPUS[0]);
                listData.Add(string.Join(", ", CPUS));
                return listData.ToArray();
            }
            public static string[] RAM()
            {
                List<string> returnvalue = new List<string>();
                hardwareInfo.RefreshMemoryList();
                int i = 0;
                foreach (Memory memory in hardwareInfo.MemoryList)
                {
                    ulong value = memory.Capacity / 1073741824;
                    returnvalue.Add(value.ToString() + " ГБ");
                    i++;
                }

                while (i < 4)
                {
                    returnvalue.Add("");
                    i++;
                }
                return returnvalue.ToArray();
            }
            public static string Antivirus()
            {
                string antivirusName = "";

                ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\SecurityCenter2", "SELECT * FROM AntiVirusProduct");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    antivirusName = queryObj["displayName"].ToString();
                    break;
                }

                return antivirusName;
            }
            public static string OS()
            {
                hardwareInfo.RefreshOperatingSystem();
                return hardwareInfo.OperatingSystem.Name;
            }

            public static string Lan()
            {
                // Получаем все сетевые интерфейсы
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                List<string> lan = new();

                foreach (NetworkInterface networkInterface in interfaces)
                {
                    // Проверяем, что интерфейс является сетевым и подключенным
                    if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                        networkInterface.OperationalStatus == OperationalStatus.Up)
                    {
                        // Получаем IP-адреса интерфейса
                        IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();
                        foreach (UnicastIPAddressInformation ipInfo in ipProperties.UnicastAddresses)
                        {
                            // Проверяем, что это IPv4-адрес и локальный адрес
                            if (ipInfo.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                                !IPAddress.IsLoopback(ipInfo.Address))
                            {
                                lan.Add(ipInfo.Address.ToString());
                            }
                        }
                    }
                }

                return String.Join(", ", lan.ToArray());
            }

        }
    }
}