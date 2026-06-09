using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ProjectHD.Editor
{
    public class EditorToolHelper
    {
        static string NAMESPACE = "ProjectHD.Data";

        public static bool ClassExists(string className)
        {
            try
            {
                var assemblyName = "Assembly-CSharp";
                var assembly = Assembly.Load(assemblyName);

                if (assembly.GetType($"{NAMESPACE}." + className) == null)
                    return false;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static List<string> GetClassNames(string filePath)
        {
            List<string> classNames = new List<string>();

            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    bool foundAttribute = false;
                    // string pattern1 = @"\[MessagePackObject\(true\)\]";
                    string pattern2 = @"\[MessagePackObject";
                    // string pattern3 = @"\[MessagePackObject\]";
                    string classOrRecordPattern = @"(class|record)\s+(\w+)";

                    while ((line = sr.ReadLine()) != null)
                    {
                        if (foundAttribute)
                        {
                            Match match = Regex.Match(line, classOrRecordPattern);
                            foundAttribute = false;
                            if (match.Success)
                            {
                                string className = match.Groups[2].Value;
                                classNames.Add(className);
                            }
                        }
                        else if (Regex.IsMatch(line, pattern2))
                        {
                            foundAttribute = true;
                        }
                        else
                        {
                            foundAttribute = false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"파일 읽기 오류: {e.Message}");
            }

            return classNames;
        }
        
        #region Utility
        
        public static bool TryGetFileList(AddressableCustomPathData tempPathData, out List<string> fileList)
        {
            fileList = new List<string>();
            foreach (var tempPath in tempPathData.PathList)
            {
                var tempFullPath = Application.dataPath + tempPath;
                switch (tempPathData.SearchOption)
                {
                    case AutoSearchOption.None:
                        break;
                    case AutoSearchOption.OnlyFiles:
                        fileList.Add(tempFullPath);
                        break;
                    case AutoSearchOption.Folder:
                        fileList.AddRange(GetAllFilesInFolder(tempFullPath, SearchOption.TopDirectoryOnly));
                        if (fileList.Count == 0)
                        {
                            Debug.LogError($"[Error] 해당 경로에는 폴더나 파일이 없습니다. ({tempFullPath})");
                        }
                        break;
                    case AutoSearchOption.SearchSubdirectories:
                        fileList = GetAllFilesInFolder(tempFullPath, SearchOption.AllDirectories);
                        if (fileList.Count == 0)
                        {
                            Debug.LogError($"[Error] 해당 경로에는 폴더나 파일이 없습니다. ({tempFullPath})");
                        }
                        break;
                }
            }
            
            bool hasFileList = fileList.Count > 0;
            return hasFileList;
        }
        
        public static List<string> GetAllFilesInFolder(string folderPath, SearchOption searchOption)
        {
            List<string> fileList = new List<string>();

            try
            {
                // 폴더 내의 모든 파일 가져오기
                string[] files = Directory.GetFiles(folderPath, "*.*", searchOption);

                foreach (string file in files)
                {
                    fileList.Add(file);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("오류 발생: " + ex.Message);
            }

            return fileList;
        }
        
        private static readonly StringBuilder masterSb = new StringBuilder();
        
        public static void AddLogline(string tempErrorLog, LogType logType)
        {
            switch (logType)
            {
                case LogType.Log:
                    Debug.Log(tempErrorLog);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(tempErrorLog);
                    break;
                case LogType.Error:
                    Debug.LogError(tempErrorLog);
                    break;
                default:
                    break;
            }
            masterSb.AppendLine(tempErrorLog);
        }
        
        private async UniTask CleanUpMemoryAsync()
        {
            await Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        #endregion
    }
}