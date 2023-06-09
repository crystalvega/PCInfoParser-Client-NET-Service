﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace PCInfoParser_Client_NET_Service
{

    class ArrayStringConverter
    {
        private const string ArraySeparator = "@@";
        private const string ElementSeparator = "##";

        public static string ToString2D(string[,] arr)
        {
            int rows = arr.GetLength(0);
            int cols = arr.GetLength(1);
            List<string> arrStrings = new List<string>(rows);
            for (int i = 0; i < rows; i++)
            {
                List<string> rowStrings = new List<string>(cols);
                for (int j = 0; j < cols; j++)
                {
                    string value = arr[i, j];
                    rowStrings.Add(EncodeValue(value));
                }
                arrStrings.Add(string.Join(ElementSeparator, rowStrings));
            }
            return string.Join(ArraySeparator, arrStrings);
        }

        public static string[,] FromString2D(string s)
        {
            string[] arrStrings = s.Split(new[] { ArraySeparator }, StringSplitOptions.RemoveEmptyEntries);
            int rows = arrStrings.Length;
            int cols = arrStrings[0].Split(new[] { ElementSeparator }, StringSplitOptions.None).Length;
            string[,] arr = new string[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                string[] rowStrings = arrStrings[i].Split(new[] { ElementSeparator }, StringSplitOptions.None);
                for (int j = 0; j < cols; j++)
                {
                    arr[i, j] = DecodeValue(rowStrings[j]);
                }
            }
            return arr;
        }

        public static string ToString3D(string[,,] arr)
        {
            int depth = arr.GetLength(0);
            int rows = arr.GetLength(1);
            int cols = arr.GetLength(2);
            List<string> matrixStrings = new List<string>(depth);
            for (int k = 0; k < depth; k++)
            {
                List<string> arrStrings = new List<string>(rows);
                for (int i = 0; i < rows; i++)
                {
                    List<string> rowStrings = new List<string>(cols);
                    for (int j = 0; j < cols; j++)
                    {
                        string value = arr[k, i, j];
                        rowStrings.Add(EncodeValue(value));
                    }
                    arrStrings.Add(string.Join(ElementSeparator, rowStrings));
                }
                matrixStrings.Add(string.Join(ArraySeparator, arrStrings));
            }
            return string.Join(Environment.NewLine + Environment.NewLine, matrixStrings);
        }

        public static string[,,] FromString3D(string s)
        {
            string[] matrixStrings = s.Split(new[] { Environment.NewLine + Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            int depth = matrixStrings.Length;
            int rows = matrixStrings[0].Split(new[] { ArraySeparator }, StringSplitOptions.RemoveEmptyEntries).Length;
            int cols = matrixStrings[0].Split(new[] { ArraySeparator }, StringSplitOptions.RemoveEmptyEntries)[0].Split(new[] { ElementSeparator }, StringSplitOptions.None).Length;
            string[,,] arr = new string[depth, rows, cols];
            for (int k = 0; k < depth; k++)
            {
                string[] arrStrings = matrixStrings[k].Split(new[] { ArraySeparator }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < rows; i++)
                {
                    string[] rowStrings = arrStrings[i].Split(new[] { ElementSeparator }, StringSplitOptions.None);
                    for (int j = 0; j < cols; j++)
                    {
                        arr[k, i, j] = DecodeValue(rowStrings[j]);
                    }
                }
            }
            return arr;
        }

        private static string EncodeValue(string value)
        {
            // Заменяем специальные символы на их эскейп-последовательности
            value = value.Replace("@", "@@");
            value = value.Replace("#", "##");
            return value;
        }

        private static string DecodeValue(string value)
        {
            // Восстанавливаем специальные символы из эскейп-последовательностей
            value = value.Replace("##", "#");
            value = value.Replace("@@", "@");
            return value;
        }
    }

    public class Cryptography
    {
        private static readonly byte[] Salt = Encoding.ASCII.GetBytes("SaltySalty");

        public static string Encrypt(string plainText, string password)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] keyBytes = new Rfc2898DeriveBytes(password, Salt).GetBytes(32);
            byte[] ivBytes = new Rfc2898DeriveBytes(password, Salt).GetBytes(16);

            using Aes aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = ivBytes;

            using var encryptor = aes.CreateEncryptor();
            byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            return Convert.ToBase64String(encryptedBytes);
        }

        public static string Decrypt(string cipherText, string password)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            byte[] keyBytes = new Rfc2898DeriveBytes(password, Salt).GetBytes(32);
            byte[] ivBytes = new Rfc2898DeriveBytes(password, Salt).GetBytes(16);

            using Aes aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = ivBytes;

            using var decryptor = aes.CreateDecryptor();
            byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }

    public class Connection
    {
        private TcpClient client;
        private NetworkStream stream;
        readonly IniFile ini;
        private readonly string[] server;
        public string clientID;
        public string responseMessage = "";
        public bool status = false;
        public string[] user;
        public bool active = false;
        private readonly string[,] general;
        private readonly string[,,] disk;
        private readonly string lan;
        private string today = "";


            public Connection(IniFile ini, string[,] general, string[,,] disk, string lan)
        {
            try
            {
                this.ini = ini;
                this.user = new string[4] { ini.GetValue("User", "ФИО"), ini.GetValue("User", "Кабинет"), ini.GetValue("User", "Организация"), ini.GetValue("User", "ID") };
                this.server = new string[3] { ini.GetValue("Server", "IP"), ini.GetValue("Server", "Port"), ini.GetValue("Server", "Password") };
                this.general = general;
                this.disk = disk;
                this.lan = lan;
                }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void Send()
        {
            while (!Connect())
            {
                Thread.Sleep(2000);
            }
            if (FirstMessage())
            {
                SendMessage("Lan: " + lan, 10);

                string gen = ArrayStringConverter.ToString2D(general);
                SendMessage("General: " + gen, 10);

                string dsk = ArrayStringConverter.ToString3D(disk);
                SendMessage("Disk: " + dsk, 10);

                SendMessage("ENDSEND", 10);
                today = ReceiveMessage();
                client.Close();
            }
        }

        public string TodayGet()
        { return today; }

        public bool Connect()
        {
            client = new TcpClient();
            try
            {
                client.Connect(server[0], Convert.ToInt32(server[1]));
                Console.WriteLine("Подключение произошло успешно!");
                stream = client.GetStream();
                return true;
            }
            catch (Exception) { return false; }
        }

        public bool FirstMessage()
        {
            try
            {
                string message = "VALIDATION;";
                foreach (string key in user)
                {
                    message += key + ";";
                }
                if (stream == null)
                    return false;

                // Send a test message to the server
                SendMessage(message, 10);

                // Receive the response from the server
                responseMessage = ReceiveMessage();
                // Check if the response message is correct
                if (responseMessage.StartsWith("VALID;"))
                {
                    ini.SetValue("User", "ID", responseMessage.Split(';')[1]);
                    ini.Save();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }


        private void SendMessage(string message, int bytes)
        {
            string encryptedMessage = Cryptography.Encrypt(message, server[2]);
            foreach (string chunk in Chunks(encryptedMessage, bytes))
            {
                byte[] data = Encoding.UTF8.GetBytes(chunk);
                stream.Write(data, 0, data.Length);
                byte[] buffer = new byte[4096];
                stream.Read(buffer, 0, buffer.Length);
            }

            byte[] endSignal = Encoding.UTF8.GetBytes("end");
            stream.Write(endSignal, 0, endSignal.Length);
            byte[] endBuffer = new byte[4096];
            stream.Read(endBuffer, 0, endBuffer.Length);
        }

        private string ReceiveMessage()
        {
            byte[] message = new byte[0];
            byte[] buffer = new byte[4096];
            int bytesTotal = 0;

            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                byte[] messageRaw = new byte[bytesRead];
                Array.Copy(buffer, messageRaw, bytesRead);

                if (Encoding.UTF8.GetString(messageRaw) != "end")
                {
                    message = message.Concat(messageRaw).ToArray();
                    stream.Write(Encoding.UTF8.GetBytes("1"), 0, 1);
                    bytesTotal += bytesRead;
                }
                else
                {
                    stream.Write(Encoding.UTF8.GetBytes("1"), 0, 1);
                    string dataReceived = Encoding.UTF8.GetString(message, 0, bytesTotal);
                    return Cryptography.Decrypt(dataReceived, server[2]);
                }
            }
        }

        private static IEnumerable<string> Chunks(string lst, int n)
        {
            for (int i = 0; i < lst.Length; i += n)
            {
                yield return lst.Substring(i, Math.Min(n, lst.Length - i));
            }
        }





        public bool Disconnect()
        {
            try
            {
                stream?.Close();
                client.Close();
                this.status = false;
                return true;

            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
