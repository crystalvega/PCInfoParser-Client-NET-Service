﻿using DocumentFormat.OpenXml.Packaging;
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
using System.Reflection;
namespace PCInfoParser_Client_NET_Service
{
    public static class Get
    {
        static readonly IHardwareInfo hardwareInfo = new HardwareInfo();
        private static RegistryKey registry = Registry.LocalMachine;
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
            string[] returnvalue = new string[2] {"",""};
            hardwareInfo.RefreshCPUList();
            string cpu = hardwareInfo.CpuList[0].Name;
            returnvalue[0] = cpu.Trim();
            returnvalue[1] = hardwareInfo.CpuList[0].MaxClockSpeed.ToString();

            return returnvalue;
        }
        private static List<string[]> CPULoad()
        {
            List<string[]> data = new List<string[]>();
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
            List<string[]> table = CPULoad();
            List<string> CPUS = new List<string>();
            string[] stringData = new string[5];
            foreach (string[] tableData in table)
            {
                if (cpu.Contains(tableData[0]))
                {
                    stringData = tableData;
                    break;
                }
            }
            List<string> listData = new List<string>(stringData);
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
    }
    public static class GetConfiguration
    {
        public static string[,] ClientInfo(string[] client)
        {
            string[,] ClientInfo = new string[3, 2] { { "Кабинет", "" }, { "LAN", "" }, { "ФИО", "" } };
            ClientInfo[0, 1] = client[0];
            ClientInfo[1, 1] = client[1];
            ClientInfo[2, 1] = client[2];
            return ClientInfo;
        }
        public static string[,] General(string[,,] smart)
        {
            string[,] General = new string[29, 2] { { "Название", "General" }, { "Монитор", "" }, { "Диагональ", "" }, { "Тип принтера", "" }, { "Модель принтера", "" }, { "ПК", "" }, { "Материнская плата", "" }, { "Процессор", "" }, { "Частота процессора", "" }, { "Баллы Passmark", "" }, { "Дата выпуска", "" }, { "Тип ОЗУ", "" }, { "ОЗУ, 1 Планка", "" }, { "ОЗУ, 2 Планка", "" }, { "ОЗУ, 3 Планка", "" }, { "ОЗУ, 4 Планка", "" }, { "Сокет", "" }, { "Диск 1", "" }, { "Состояние диска 1", "" }, { "Диск 2", "" }, { "Состояние диска 2", "" }, { "Диск 3", "" }, { "Состояние диска 3", "" }, { "Диск 4", "" }, { "Состояние диска 4", "" }, { "Операционная система", "" }, { "Антивирус", "" }, { "CPU Под замену", "" }, { "Все CPU под сокет", "" } };
            string[] display = Get.Display();
            string[] printer = Get.Printer();
            string typepc = Get.PCType();
            string motherboard = Get.Motherboard();
            string[] cpu = Get.CPU();
            string[] upgrade = Get.CPUUpgrade(cpu[0]);
            string os = Get.OS();
            string[] ram = Get.RAM();
            string antivirus = Get.Antivirus();

            General[1, 1] = display[0];
            General[2, 1] = display[1];
            General[3, 1] = printer[1];
            General[4, 1] = printer[0];
            General[5, 1] = typepc;
            General[6, 1] = motherboard;
            General[7, 1] = upgrade[0];
            General[8, 1] = cpu[1];
            General[9, 1] = upgrade[1];
            General[10, 1] = upgrade[2];
            General[11, 1] = upgrade[4];
            General[12, 1] = ram[0];
            General[13, 1] = ram[1];
            General[14, 1] = ram[2];
            General[15, 1] = ram[3];
            General[16, 1] = upgrade[3];
            General[17, 1] = smart[0, 1, 1];
            General[18, 1] = smart[0, 7, 1];
            General[19, 1] = smart[1, 1, 1];
            General[20, 1] = smart[1, 7, 1];
            General[21, 1] = smart[2, 1, 1];
            General[22, 1] = smart[2, 7, 1];
            General[23, 1] = smart[3, 1, 1];
            General[24, 1] = smart[3, 7, 1];
            General[25, 1] = os;
            General[26, 1] = antivirus;
            General[27, 1] = upgrade[5];
            General[28, 1] = upgrade[6];
            return General;
        }
        public static string[,,] Disk()
        {
            string[,] DiskPreset = new string[8, 2] { { "Название", "Disk" }, { "Наименование", "" }, { "Прошивка", "" }, { "Размер", "" }, { "Время работы", "" }, { "Включён", "" }, { "Температура", "" }, { "Состояние", "" } };
            string[,,] returnvalue = new string[4,8,2];
            GetSmart smart = new GetSmart();
            List<string[]> smartData = smart.Get();
            for (int i = 0; i < 4; i++)
            {
                returnvalue[i, 0, 1] = DiskPreset[0, 1];
                for (int j = 0; j < 7; j++)
                {
                    returnvalue[i, j + 1, 1] = smartData[i][j];
                    returnvalue[i, j, 0] = DiskPreset[j, 0];
                }
                returnvalue[i, 7, 0] = DiskPreset[7, 0];
            }
            return returnvalue;
        }
    }
    internal class GetSmart
    {
        List<string[]> smartparse = new List<string[]>();
        internal GetSmart()
        {
            List<string> smart = new List<string>();
            string directory = Path.Combine(Command.AssemblyDirectory(), "DiskInfo");
            string exePath = Path.Combine(directory, "DiskInfo32.exe");
            string arguments = "/copyexit";
            string[] values = new string[7] { "Model", "Power On Hours", "Power On Count", "Firmware", "Disk Size", "Temperature", "Health Status" };
            Process.Start(exePath, arguments).WaitForExit();

            string[] lines = File.ReadAllLines(Path.Combine(directory, "diskinfo.txt"));
            bool start = false;
            bool endlinecheck = false;
            foreach (string line in lines)
            {
                if (smartparse.Count == 4) break;
                if (start)
                {
                    if (line.StartsWith(" (0"))
                    {
                        if (endlinecheck) endlinecheck = false;
                        else
                        {
                            smartparse.Add(smart.ToArray());
                            smart.Clear();
                        }
                    }
                    foreach(string val in values) if (line.Contains(val) && line.Contains(" : ")) smart.Add(line.Split(':')[1].Trim());
                }
                if (line.Contains("Disk List")) endlinecheck = true;
                if (endlinecheck && line == "----------------------------------------------------------------------------") start = true;
            }
            if (smartparse.Count != 4) smartparse.Add(smart.ToArray());
            while (smartparse.Count != 4) smartparse.Add(new string[7] { "","","","","","", "" });
        }
        public List<string[]> Get()
        {
            return smartparse;
        }
    }
}