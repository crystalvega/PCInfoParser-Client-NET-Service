using Microsoft.Win32;
using System;
using System.Management;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCInfoParser_Client_NET_Service
{
    public class GetDisplay
    {
        static RegistryKey registry = Registry.LocalMachine;
        static byte[] OpenRegistryA(string dir)
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
        public static object[] Get(string dir)
        {
            byte[] edid_hex = OpenRegistryA(dir);
            object[] edid_result = new object[] { "Не найдено", "Не найдено" };
            if (edid_hex != null)
            {
                string edid_hex_string = BitConverter.ToString(edid_hex).Replace("-", "");
                EDIDParser.EDID parser = new EDIDParser.EDID(edid_hex);
                //pyedid.Edid edid = pyedid.EdidExtensions.Parse(edid_hex_string);
                //edid_result[0] = edid.Manufacturer + " " + edid.Name;
                //edid_result[1] = Math.Round(Math.Round(Vector2.Distance(new Vector2(0, 0), new Vector2(edid.Width, edid.Height)), 1) * 0.393701, 1);
                //string json_str = edid.ToJsonString();
            }
            return edid_result;
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
            return General;
        }
        public static string[,] Disk(int number = 0)
        {

            string[,] Disk = new string[8, 2] { { "Название", "Disk" }, { "Наименование", "" }, { "Прошивка", "" }, { "Размер", "" }, { "Время работы", "" }, { "Включён", "" }, { "Температура", "" }, { "Состояние", "" } };



            return Disk;
        }
    }
    internal class GetSmart
    {
        internal GetSmart()
        {

        }
    }
}
