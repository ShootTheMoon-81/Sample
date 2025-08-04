using System;
using System.IO;
using UnityEngine;

//Namespace 협의 필요.
namespace Log
{
    public static class LogExporter
    {
        private const string DefaultExportFileName = "Log_";
        
        private static readonly string DefaultExportPath = Path.GetFullPath(Path.Combine(Application.dataPath, @"../../../Log/"));

        internal static void Log(string errorTag, string url, string content)
        {
            if (errorTag == null || url == null || content == null)
            {
                return;
            }
            
            string[] lines = content.Split('\n');
            string emptyTag = GetEmptyString(errorTag.Length);
            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0)
                {
                    AppendSingleLine(errorTag, url, lines[i].Trim());
                    
                    continue;
                }

                AppendSingleLine(emptyTag, url, lines[i].Trim());
            }
        }
        
        private static string GetEmptyString(int length)
        {
            string result = string.Empty;
            for (int i = 0; i < length; i++)
            {
                result += " ";
            }

            return result;
        }
        
        private static void AppendSingleLine(string errorTag, string url, string content)
        {
            string filePath = GetOrCreateFile();
            int lineCount = File.ReadAllLines(filePath).Length;
            FileStream fileStream = new(filePath, FileMode.Append);
            StreamWriter writer = new(fileStream);
            writer.WriteLine($"{(lineCount / 3):D5}  {url}  {GetCurrentTime()}  {errorTag}");
            writer.WriteLine($"{content}");
            writer.WriteLine();
            writer.Flush();
            writer.Dispose();
            fileStream.Dispose();
        }
        
        private static string GetOrCreateFile()
        {
            string fileName = $"{DefaultExportFileName}{DateTime.Now:yyyy-MM-dd}.txt";
            string filePath = Path.Combine(DefaultExportPath, fileName);

            if (!Directory.Exists(DefaultExportPath))
            {
                Directory.CreateDirectory(DefaultExportPath);
            }

            if (File.Exists(filePath))
            {
                return filePath;
            }

            FileStream fileStream = new(filePath, FileMode.Append);
            StreamWriter writer = new(fileStream);
            writer.WriteLine($"Created by :: {AccountManager.Instance.AccountIdToken}");
            writer.WriteLine($"Created on :: {GetCurrentTime()}");
            writer.WriteLine();
            writer.Flush();
            writer.Dispose();
            fileStream.Dispose();

            return filePath;
        }
        
        private static string GetCurrentTime()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        internal static void Clear()
        {
            if (File.Exists(GetOrCreateFile()))
            {
                File.Delete(GetOrCreateFile());
            }
        }
    }
}