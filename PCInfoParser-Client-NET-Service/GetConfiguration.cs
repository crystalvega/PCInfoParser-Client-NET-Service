using Microsoft.Win32;
using System;
using System.Management;
using Hardware.Info;
using EDIDParser;
using System.Drawing.Printing;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Linq;
using System.Runtime.InteropServices;
using Ardalis.SmartEnum;
using System.ComponentModel;
using System.IO;
using System.Text;

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
                string name = "EDID";//(string)rawKeyA.GetValue(i.ToString());
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
            string[] wordsToReplace = { "12th Gen", "11th Gen", "10th Gen", "(R)", "(TM)", "CPU", "with Radeon Vega Graphics", "Mobile", "Processor", "Quad-Core", "(tm)" };
            string[] returnvalue = new string[2] {"",""};
            hardwareInfo.RefreshCPUList();
            string cpu = hardwareInfo.CpuList[0].Name;
            for (int i = 0; i < wordsToReplace.Length; i++)
            {
                cpu = cpu.Replace(wordsToReplace[i], "");
            }
            returnvalue[0] = cpu.Trim();
            returnvalue[1] = hardwareInfo.CpuList[0].MaxClockSpeed.ToString();

            return returnvalue;
        }
        private static List<string[]> CPULoad()
        {
            List<string[]> data = new List<string[]>();

            using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Open("db.xlsx", false))
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
                if (tableData[0] == cpu)
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
        public static string[,] General()
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
            General[7, 1] = cpu[0];
            General[8, 1] = cpu[1];
            General[9, 1] = upgrade[1];
            General[10, 1] = upgrade[2];
            General[11, 1] = upgrade[4];
            General[12, 1] = ram[0];
            General[13, 1] = ram[1];
            General[14, 1] = ram[2];
            General[15, 1] = ram[3];
            General[16, 1] = upgrade[3];
            General[17, 1] = "";
            General[18, 1] = "";
            General[19, 1] = "";
            General[20, 1] = "";
            General[21, 1] = "";
            General[22, 1] = "";
            General[23, 1] = "";
            General[24, 1] = "";
            General[25, 1] = os;
            General[26, 1] = antivirus;
            General[27, 1] = upgrade[5];
            General[28, 1] = upgrade[6];
            return General;
        }
        public static List<string[,]> Disk()
        {
            string[,] Disk = new string[8, 2] { { "Название", "Disk" }, { "Наименование", "" }, { "Прошивка", "" }, { "Размер", "" }, { "Время работы", "" }, { "Включён", "" }, { "Температура", "" }, { "Состояние", "" } };
            List<string[,]> returnvalue = new List<string[,]>();
            GetSmart smart = new GetSmart();
            returnvalue.Add(Disk);



            return returnvalue;
        }
    }
    internal class GetSmart
    {
        internal GetSmart()
        {
            SmartI smart = new SmartI();
        }
    }
    public class SmartI
    {
        private readonly Dictionary<int, Smart> _smartInfo = new Dictionary<int, Smart>();
        public readonly HashSet<int> FutureReserchUnknownAttributes = new HashSet<int>();
        private static bool Is64Bit => IntPtr.Size == 8;
        private static uint OffsetSize => Is64Bit ? 8u : 6u;
        public Dictionary<int, Smart> SmartInfo
        {
            get
            {
                if (_smartInfo.Count == 0)
                    GetSmartInfo();
                return _smartInfo;
            }
        }
        public IEnumerable<string> GetDriveReadyList => from driveInfo
                in DriveInfo.GetDrives()
                                                        where driveInfo.IsReady
                                                        select driveInfo.Name;
        private Dictionary<string, string> LogicalDrives
        {
            get
            {
                var logicalDrives = new Dictionary<string, string>();
                foreach (var drive in GetDriveReadyList)
                {
                    var sb = new StringBuilder(1024);
                    if (NativeWin32.GetVolumeNameForVolumeMountPoint(drive, sb, sb.Capacity))
                        logicalDrives.Add(drive.Replace("\\", ""), sb.ToString());
                }
                return logicalDrives;
            }
        }
        private Dictionary<string, List<NativeWin32.DISK_EXTENT>> DiskNumbers
        {
            get
            {
                var diskNumbers = new Dictionary<string, List<NativeWin32.DISK_EXTENT>>();
                foreach (var ld in LogicalDrives)
                {
                    var dkexts = new List<NativeWin32.DISK_EXTENT>();
                    var hFile = NativeWin32.CreateFile(@"\\.\" + ld.Key, NativeWin32.GENERIC_READ,
                        NativeWin32.FILE_SHARE_READ | NativeWin32.FILE_SHARE_WRITE, IntPtr.Zero,
                        NativeWin32.OPEN_EXISTING, 0, IntPtr.Zero);
                    if (hFile == (IntPtr)NativeWin32.INVALID_HANDLE_VALUE)
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    var size = 1024;
                    var buffer = Marshal.AllocHGlobal(size);
                    var alloced = buffer;
                    var bytesReturned = 0;
                    try
                    {
                        if (!NativeWin32.DeviceIoControl(hFile,
                            NativeWin32.IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS, IntPtr.Zero, 0, buffer, size,
                            out bytesReturned, IntPtr.Zero))
                        {
                        }
                    }
                    catch (Exception e)
                    {
                        var m = e.Message;
                    }
                    finally
                    {
                        NativeWin32.CloseHandle(hFile);
                    }
                    if (bytesReturned > 0)
                    {
                        var vde = new NativeWin32.VOLUME_DISK_EXTENTS();
                        var dextent = new NativeWin32.DISK_EXTENT();
                        Marshal.PtrToStructure(buffer, vde);
                        buffer = new IntPtr(buffer.ToInt64() + Marshal.SizeOf(vde));
                        for (var i = 0; i < vde.NumberOfDiskExtents; i++)
                        {
                            dextent =
                                (NativeWin32.DISK_EXTENT)Marshal.PtrToStructure(buffer,
                                    typeof(NativeWin32.DISK_EXTENT));
                            dkexts.Add(dextent);
                            buffer = new IntPtr(buffer.ToInt64() + Marshal.SizeOf(dextent));
                        }
                    }
                    Marshal.FreeHGlobal(alloced);
                    diskNumbers.Add(ld.Key, dkexts);
                }
                return diskNumbers;
            }
        }
        private (string Vendor, string Product) GetVendorProduct(string data)
        {
            var ven = data.Substring(data.IndexOf("VEN_", StringComparison.CurrentCultureIgnoreCase) + 4);
            var end = ven.SuperIndexOf("&", 1);
            if (end == -1)
                end = ven.SuperIndexOf("\\", 1);
            if (end == -1)
                return (null, null);
            var VendorStr = ven.Substring(0, end).ToUpper();
            var pro = data.Substring(data.IndexOf("PROD_", StringComparison.CurrentCultureIgnoreCase) + 5);
            var end1 = pro.SuperIndexOf("&", 1);
            if (end1 == -1)
                end1 = pro.SuperIndexOf("\\", 1);
            if (end1 == -1)
                return (VendorStr, null);
            var ProductStr = pro.Substring(0, end1).ToUpper();
            end1 = ProductStr.SuperIndexOf("\\", 1);
            if (end1 == -1)
                return (VendorStr, ProductStr);
            ProductStr = pro.Substring(0, end1).ToUpper();
            return (VendorStr, ProductStr);
        }
        private int GetIndex(string ven, string pro)
        {
            for (var i = 0; i < _smartInfo.Count; ++i)
            {
                var v = _smartInfo[i];
                if (ven == v.Vendor && pro == v.Product)
                    return i;
            }
            return -1;
        }
        /// <summary>
        ///     Item    Data
        ///     0 and 1 Unknown usually zero
        ///     2       Attribute
        ///     3       Status
        ///     4       Unknown usually zero
        ///     5       Value
        ///     6       Worst
        ///     7,8     Raw Value
        ///     9,10,11 Unknown usually zero
        /// </summary>
        private void GetSmartInfo()
        {
            try
            {
                var pdski = new PDiskInformation();
                _smartInfo.Clear();
                var driveletterlist = new List<string>();
                var ldSearcher = new ManagementObjectSearcher("select * from Win32_LogicalDisk");
                foreach (ManagementObject drive in ldSearcher.Get())
                {
                    var drvn = drive["Name"].ToString().Trim();
                    driveletterlist.Add(drvn);
                }
                var wdSearcher = new ManagementObjectSearcher("select * from Win32_DiskDrive");
                var Index = 0;
                foreach (ManagementObject drive in wdSearcher.Get())
                {
                    var smart = new Smart();
                    smart.Model = drive["Model"].ToString().Trim();
                    var PNPDeviceID = drive["PNPDeviceID"].ToString().Trim();
                    var (vendor, product) = GetVendorProduct(PNPDeviceID);
                    smart.Vendor = vendor;
                    smart.Product = product;
                    smart.Type = drive["InterfaceType"].ToString().Trim();
                    smart.Disk = drive["Index"].ToString().Trim();
                    smart.Size = drive["Size"].ToString().Trim();
                    smart.Status = drive["Status"].ToString().Trim();
                    _smartInfo.Add(Index, smart);
                    Index++;
                }
                var pmsearcher = new ManagementObjectSearcher("select * from Win32_PhysicalMedia");
                foreach (ManagementObject drive in pmsearcher.Get())
                {
                    var dsk = drive["Tag"].ToString().Trim().Last().ToString().ToInt32();
                    _smartInfo[dsk].Serial = drive["SerialNumber"] == null ? "None" : drive["SerialNumber"].ToString().Trim();
                }
                var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM MSStorageDriver_ATAPISmartData");
                foreach (ManagementObject data in searcher.Get())
                {
                    var pt = data.Properties;
                    var VendorSpecific = (byte[])data.Properties["VendorSpecific"].Value;
                    var vsData = data["InstanceName"].ToString().Trim();
                    var (vendor, product) = GetVendorProduct(vsData);
                    var idx = GetIndex(vendor, product);
                    if (idx != -1)
                        for (var offset = 2; offset < VendorSpecific.Length; offset += 12)
                        {
                            var a = FromBytes<SmartAttribute>(VendorSpecific, offset);
                            int id = VendorSpecific[offset];
                            try
                            {
                                int value;
                                if (id == (int)SmartAttributeTypes.Temperature)
                                    value = Convert.ToInt32(a.VendorData[0]);
                                else
                                    value = BitConverter.ToInt32(a.VendorData, 0);
                                if (id == 0) continue;
                                if (_smartInfo[idx].AttributesTemplate.ContainsKey(id))
                                {
                                    _smartInfo[idx].Attributes.Add(id, _smartInfo[idx].AttributesTemplate[id]);
                                    var attr = _smartInfo[idx].Attributes[id];
                                    attr.Current = value;
                                    attr.FailureImminent = a.FailureImminent;
                                    attr.Advisory = a.Advisory;
                                    attr.OnlineDataCollection = a.OnlineDataCollection;
                                    attr.Status = a.FailureImminent == false ? "Ok" : "Fail";
                                }
                            }
                            catch
                            {
                                if (!FutureReserchUnknownAttributes.Contains(id))
                                    FutureReserchUnknownAttributes.Add(id);
                            }
                        }
                }
                var vp = GetVolumePaths();
                for (var idx = 0; idx < _smartInfo.Count; idx++)
                {
                    var driveletter = "";
                    foreach (var drive in driveletterlist)
                        if (_smartInfo[idx].Disk == vp[drive].DiskNumbers[0].ToString())
                        {
                            driveletter = drive;
                            break;
                        }
                    if (driveletter != "")
                    {
                        _smartInfo[idx].Drive = driveletter;
                        _smartInfo[idx].MediaType = new PDMediaTypesStr().types[pdski.PDiskInfo[_smartInfo[idx].Disk].MediaType];
                    }
                }
            }
            catch (ManagementException e)
            {
                throw new Exception($"WMI data error: {e.Message}");
            }
        }
        private T FromBytes<T>(byte[] bytearray, int offset)
        {
            var ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(12);
                Marshal.Copy(bytearray, offset, ptr, 12);
                return (T)Marshal.PtrToStructure(ptr, typeof(T));
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }
        private Dictionary<string, SetupDi.VOLUME_INFO> GetVolumePaths()
        {
            var _volumepaths = new Dictionary<string, SetupDi.VOLUME_INFO>();
            var classGuid = NativeWin32.GUID_DEVINTERFACE.GUID_DEVINTERFACE_VOLUME;
            var hDevInfo = NativeWin32.SetupDiGetClassDevs(ref classGuid, null, IntPtr.Zero,
                NativeWin32.DICFG.DEVICEINTERFACE | NativeWin32.DICFG.PRESENT);
            if (hDevInfo == (IntPtr)NativeWin32.INVALID_HANDLE_VALUE)
                throw new Exception("Read hardware information error.");
            var devIndex = 0;
            do
            {
                var dia = new NativeWin32.SP_DEVICE_INTERFACE_DATA();
                dia.cbSize = (uint)Marshal.SizeOf(typeof(NativeWin32.SP_DEVICE_INTERFACE_DATA));
                if (NativeWin32.SetupDiEnumDeviceInterfaces(hDevInfo, IntPtr.Zero, classGuid, (uint)devIndex, ref dia))
                {
                    var didd = new NativeWin32.SP_DEVICE_INTERFACE_DETAIL_DATA();
                    didd.cbSize = OffsetSize;
                    var devInfoData = new NativeWin32.SP_DEVINFO_DATA();
                    devInfoData.cbSize = (uint)Marshal.SizeOf(typeof(NativeWin32.SP_DEVINFO_DATA));
                    uint nRequiredSize = 0;
                    uint nBytes = 1024;
                    if (NativeWin32.SetupDiGetDeviceInterfaceDetail(hDevInfo, ref dia, ref didd, nBytes, ref nRequiredSize, ref devInfoData))
                    {
                        var sb = new StringBuilder(1024);
                        if (NativeWin32.GetVolumeNameForVolumeMountPoint(didd.devicePath + @"\", sb, sb.Capacity))
                        {
                            var cv = sb.ToString();
                            var di = new SetupDi.VOLUME_INFO();
                            foreach (var V in LogicalDrives)
                                if (V.Value.IndexOf(cv, StringComparison.OrdinalIgnoreCase) != -1)
                                {
                                    di.Drive = V.Key;
                                    di.VolumeMountPoint = cv;
                                    di.DevicePath = didd.devicePath;
                                    foreach (var de in DiskNumbers[V.Key])
                                    {
                                        di.DiskNumbers.Add(de.DiskNumber);
                                        di.ExtentLengths.Add(de.ExtentLength);
                                        di.StartingOffsets.Add(de.StartingOffset);
                                    }
                                    _volumepaths.Add(di.Drive.Trim(), di);
                                    break;
                                }
                        }
                    }
                    else
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                }
                else
                {
                    break;
                }
                devIndex++;
            } while (true);
            NativeWin32.SetupDiDestroyDeviceInfoList(hDevInfo);
            return _volumepaths;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct SmartAttribute
        {
            public readonly SmartAttributeTypes AttributeType;
            public readonly ushort Flags;
            public readonly byte Value;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public readonly byte[] VendorData;
            public bool Advisory => (Flags & 0x1) == 0x0;
            public bool FailureImminent => (Flags & 0x1) == 0x1;
            public bool OnlineDataCollection => (Flags & 0x2) == 0x2;
        }
    }

    public class Smart
    {
        public readonly Dictionary<int, SmartData> Attributes = new Dictionary<int, SmartData>();
        public readonly Dictionary<int, SmartData> AttributesTemplate = new Dictionary<int, SmartData>
        {
            {0, new SmartData("Invalid")},
            {1, new SmartData("read error rate")},
            {2, new SmartData("Throughput performance")},
            {3, new SmartData("Spin up time")},
            {4, new SmartData("Start/Stop count")},
            {5, new SmartData("Reallocated sector count")},
            {6, new SmartData("Read channel margin")},
            {7, new SmartData("Seek error rate")},
            {8, new SmartData("Seek timer performance")},
            {9, new SmartData("Power-on hours count")},
            {10, new SmartData("Spin up retry count")},
            {11, new SmartData("Calibration retry count")},
            {12, new SmartData("Power cycle count")},
            {13, new SmartData("Soft read error rate")},
            {22, new SmartData("Current Helium Level")},
            {160, new SmartData("Uncorrectable sector count read or write")},
            {161, new SmartData("Remaining spare block percentage")},
            {164, new SmartData("Total Erase Count")},
            {165, new SmartData("Maximum Erase Count")},
            {166, new SmartData("Minimum Erase Count")},
            {167, new SmartData("Average Erase Count")},
            {168, new SmartData("Max NAND Erase Count from specification")},
            {169, new SmartData("Remaining life percentage")},
            {170, new SmartData("Available Reserved Space")},
            {171, new SmartData("SSD Program Fail Count")},
            {172, new SmartData("SSD Erase Fail Count")},
            {173, new SmartData("SSD Wear Leveling Count")},
            {174, new SmartData("Unexpected Power Loss Count")},
            {175, new SmartData("Power Loss Protection Failure")},
            {176, new SmartData("Erase Fail Count")},
            {177, new SmartData("Wear Range Delta")},
            {178, new SmartData("Used Reserved Block Count (Chip)")},
            {179, new SmartData("Used Reserved Block Count (Total)")},
            {180, new SmartData("Unused Reserved Block Count Total")},
            {181, new SmartData("Program Fail Count Total or Non 4K Aligned Access Count")},
            {182, new SmartData("Erase Fail Count")},
            {183, new SmartData("SATA Down shift Error Count")},
            {184, new SmartData("End-to-End error")},
            {185, new SmartData("Head Stability")},
            {186, new SmartData("Induced Op Vibration Detection")},
            {187, new SmartData("Reported Uncorrectable Errors")},
            {188, new SmartData("Command Timeout")},
            {189, new SmartData("High Fly Writes")},
            {190, new SmartData("Temperature Difference from 100")},
            {191, new SmartData("G-sense error rate")},
            {192, new SmartData("Power-off retract count")},
            {193, new SmartData("Load/Unload cycle count")},
            {194, new SmartData("Temperature")},
            {195, new SmartData("Hardware ECC recovered")},
            {196, new SmartData("Reallocation count")},
            {197, new SmartData("Current pending sector count")},
            {198, new SmartData("Off-line scan uncorrectable count")},
            {199, new SmartData("UDMA CRC error rate")},
            {200, new SmartData("Write error rate")},
            {201, new SmartData("Soft read error rate")},
            {202, new SmartData("Data Address Mark errors")},
            {203, new SmartData("Run out cancel")},
            {204, new SmartData("Soft ECC correction")},
            {205, new SmartData("Thermal asperity rate (TAR)")},
            {206, new SmartData("Flying height")},
            {207, new SmartData("Spin high current")},
            {208, new SmartData("Spin buzz")},
            {209, new SmartData("Off-line seek performance")},
            {211, new SmartData("Vibration During Write")},
            {212, new SmartData("Shock During Write")},
            {220, new SmartData("Disk shift")},
            {221, new SmartData("G-sense error rate")},
            {222, new SmartData("Loaded hours")},
            {223, new SmartData("Load/unload retry count")},
            {224, new SmartData("Load friction")},
            {225, new SmartData("Load/Unload cycle count")},
            {226, new SmartData("Load-in time")},
            {227, new SmartData("Torque amplification count")},
            {228, new SmartData("Power-off retract count")},
            {230, new SmartData("Life Curve Status")},
            {231, new SmartData("SSD Life Left")},
            {232, new SmartData("Endurance Remaining")},
            {233, new SmartData("Media Wear out Indicator")},
            {234, new SmartData("Average Erase Count AND Maximum Erase Count")},
            {235, new SmartData("Good Block Count AND System Free Block Count")},
            {240, new SmartData("Head flying hours")},
            {241, new SmartData("Lifetime Writes From Host GiB")},
            {242, new SmartData("Lifetime Reads From Host GiB")},
            {243, new SmartData("Total LBAs Written Expanded")},
            {244, new SmartData("Total LBAs Read Expanded")},
            {249, new SmartData("NAND Writes GiB")},
            {250, new SmartData("Read error retry rate")},
            {251, new SmartData("Minimum Spares Remaining")},
            {252, new SmartData("Newly Added Bad Flash Block")},
            {254, new SmartData("Free Fall Protection")}
        };
        public string Status { get; set; }
        public string Model { get; set; }
        public string Vendor { get; set; }
        public string Product { get; set; }
        public string Type { get; set; }
        public string Serial { get; set; }
        public string Disk { get; set; }
        public string Drive { get; set; }
        public string MediaType { get; set; }
        public string Size { get; set; }
        public class SmartData
        {
            public SmartData(string attributeName)
            {
                Attribute = attributeName;
            }
            public string Attribute { get; set; }
            public int Current { get; set; }
            public string Status { get; set; }
            public bool Advisory { get; set; }
            public bool FailureImminent { get; set; }
            public bool OnlineDataCollection { get; set; }
        }
    }

    public enum SmartAttributeTypes : byte
    {
        Invalid = 0,
        ReadErrorRate = 1,
        ThroughputPerformance = 2,
        SpinUpTime = 3,
        StartStopCount = 4,
        ReallocatedSectorsCount = 5,
        ReadChannelMargin = 6,
        SeekErrorRate = 7,
        SeekTimePerformance = 8,
        PowerOnHoursCount = 9,
        SpinRetryCount = 10,
        CalibrationRetryCount = 11,
        PowerCycleCount = 12,
        SoftReadErrorRate = 13,
        CurrentHeliumLevel = 22,
        UncorrectableSectorCountReadOrWrite = 160,
        RemainingSpareBlockPercentage = 161,
        TotalEraseCount = 164,
        MaximumEraseCount = 165,
        MinimumEraseCount = 166,
        AverageEraseCount = 167,
        MaxNANDEraseCountFromSpecification = 168,
        RemainingLifePercentage = 169,
        AvailableReservedSpace = 170,
        SSDProgramFailCount = 171,
        SSDEraseFailCount = 172,
        SSDWearLevelingCount = 173,
        UnexpectedPowerLossCount = 174,
        PowerLossProtectionFailure = 175,
        EraseFailCount = 176,
        WearRangeDelta = 177,
        UsedReservedBlockCountChip = 178,
        UsedReservedBlockCountTotal = 179,
        UnusedReservedBlockCountTotal = 180,
        ProgramFailCountTotalorNon4KAlignedAccessCount = 181,
        EraseFailCountSamsung = 182,
        SATADownshiftErrorCount = 183,
        EndtoEnderror = 184,
        HeadStability = 185,
        InducedOpVibrationDetection = 186,
        ReportedUncorrectableErrors = 187,
        CommandTimeout = 188,
        HighFlyWrites = 189,
        TemperatureDifferencefrom100 = 190,
        Gsenseerrorrate = 191,
        PoweroffRetractCount = 192,
        LoadCycleCount = 193,
        Temperature = 194,
        HardwareECCRecovered = 195,
        ReallocationEventCount = 196,
        CurrentPendingSectorCount = 197,
        UncorrectableSectorCount = 198,
        UltraDMACRCErrorCount = 199,
        MultiZoneErrorRate = 200,
        OffTrackSoftReadErrorRate = 201,
        DataAddressMarkerrors = 202,
        RunOutCancel = 203,
        SoftECCCorrection = 204,
        ThermalAsperityRateTAR = 205,
        FlyingHeight = 206,
        SpinHighCurrent = 207,
        SpinBuzz = 208,
        OfflineSeekPerformance = 209,
        VibrationDuringWrite = 211,
        ShockDuringWrite = 212,
        DiskShift = 220,
        GSenseErrorRate = 221,
        LoadedHours = 222,
        LoadUnloadRetryCount = 223,
        LoadFriction = 224,
        LoadUnloadCycleCount = 225,
        LoadInTime = 226,
        TorqueAmplificationCount = 227,
        PowerOffRetractCycle = 228,
        LifeCurveStatus = 230,
        SSDLifeLeft = 231,
        EnduranceRemaining = 232,
        MediaWearoutIndicator = 233,
        AverageEraseCountANDMaximumEraseCount = 234,
        GoodBlockCountANDSystemFreeBlockCount = 235,
        HeadFlyingHours = 240,
        LifetimeWritesFromHostGiB = 241,
        LiftetimeReadsFromHostGiB = 242,
        TotalLBAsWrittenExpanded = 243,
        TotalLBAsReadExpanded = 244,
        NANDWrites1GiB = 249,
        ReadErrorRetryRate = 250,
        MinimumSparesRemaining = 251,
        NewlyAddedBadFlashBlock = 252,
        FreeFallProtection = 254
    }
}
