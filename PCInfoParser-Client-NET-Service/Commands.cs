using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.ServiceProcess;

namespace PCInfoParser_Client_NET_Service
{
	public class IniFile
	{
		private readonly Dictionary<string, Dictionary<string, string>> data = new Dictionary<string, Dictionary<string, string>>();
		private readonly string fileName;

		public IniFile(string fileName)
		{
			this.fileName = fileName;

			if (File.Exists(fileName))
			{
				Load();
			}
		}

		public string GetValue(string section, string key)
		{
			if (data.TryGetValue(section, out Dictionary<string, string> sectionData))
			{
				if (sectionData.TryGetValue(key, out string value))
				{
					return value;
				}
			}

			return null;
		}

		public void SetValue(string section, string key, string value)
		{
			if (!data.TryGetValue(section, out Dictionary<string, string> sectionData))
			{
				sectionData = new Dictionary<string, string>();
				data[section] = sectionData;
			}

			sectionData[key] = value;
		}

		public void Load()
		{
			data.Clear();

			string currentSection = null;

			foreach (string line in File.ReadAllLines(fileName))
			{
				string trimmedLine = line.Trim();
				if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
				{
					currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
					if (!data.ContainsKey(currentSection))
					{
						data[currentSection] = new Dictionary<string, string>();
					}
				}
				else if (!string.IsNullOrEmpty(trimmedLine))
				{
					string[] parts = trimmedLine.Split(new char[] { '=' }, 2);
					if (parts.Length > 1)
					{
						string currentKey = parts[0].Trim();
						string currentValue = parts[1].Trim();
						if (data.TryGetValue(currentSection, out Dictionary<string, string> sectionData))
						{
							sectionData[currentKey] = currentValue;
						}
					}
				}
			}
		}

		public void Save()
		{
			List<string> lines = new List<string>();
			foreach (KeyValuePair<string, Dictionary<string, string>> section in data)
			{
				lines.Add("[" + section.Key + "]");
				foreach (KeyValuePair<string, string> keyValuePair in section.Value)
				{
					lines.Add(keyValuePair.Key + "=" + keyValuePair.Value);
				}
				lines.Add("");
			}

			File.WriteAllLines(fileName, lines.ToArray());
		}
	}


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
			using ServiceController controller = new(servicename);
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
		internal static void Stop(string servicename)
		{
			if (!IsInstalled(servicename)) return;
			using ServiceController controller = new(servicename);
			try
			{
				if (controller.Status != ServiceControllerStatus.Stopped)
				{
					controller.Stop();
					controller.WaitForStatus(ServiceControllerStatus.Stopped,
						TimeSpan.FromSeconds(10));
				}
			}
			catch
			{
				throw;
			}
		}
	}
	internal static class Command
	{
		public static string AssemblyDirectory()
		{
			string codeBase = Assembly.GetExecutingAssembly().CodeBase;
			UriBuilder uri = new(codeBase);
			string path = Uri.UnescapeDataString(uri.Path);
			return Path.GetDirectoryName(path);
		}
		public static void FileSave(string filename, string[,] arr)
		{
			string Directory = Path.Combine(AssemblyDirectory(), filename);
			StreamWriter writer = new StreamWriter(Directory);

			// Записываем размеры массива в первую строку файла
			writer.WriteLine(arr.GetLength(0));
			writer.WriteLine(arr.GetLength(1));

			// Записываем элементы массива в следующие строки файла
			for (int i = 0; i < arr.GetLength(0); i++)
			{
				for (int j = 0; j < arr.GetLength(1); j++)
				{
					writer.Write(arr[i, j] + "§");
				}
				writer.WriteLine();
			}

			// Закрываем файл
			writer.Close();
		}
		public static void FileRemove(string filename)
		{
			string Directory = Path.Combine(AssemblyDirectory(), filename);
			if (File.Exists(Directory))
			{
				File.Delete(Directory);
			}
		}
		public static void FileSave(string filename, string[,,] arr)
		{
			string Directory = Path.Combine(AssemblyDirectory(), filename);
			StreamWriter writer = new StreamWriter(Directory);

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
						writer.Write(arr[i, j, k] + "§");
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
			string systemDirectory = Path.Combine(AssemblyDirectory(), "DiskInfo");
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
					using FileStream fileStream = new(exePath, FileMode.Create);
					stream.CopyTo(fileStream);
				}
				// Копируем содержимое папок из внедренных ресурсов в системный каталог
				string graphPath = Path.Combine(dialogFolder, "Graph.html");
				string englishPath = Path.Combine(languageFolder, "English.lang");
				using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PCInfoParser_Client_NET_Service.CdiResource.dialog.Graph.html"))
				{
					using FileStream fileStream = new(graphPath, FileMode.Create);
					stream.CopyTo(fileStream);
				}
				using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PCInfoParser_Client_NET_Service.CdiResource.language.English.lang"))
				{
					using FileStream fileStream = new(englishPath, FileMode.Create);
					stream.CopyTo(fileStream);
				}
			}
		}
	}
}
