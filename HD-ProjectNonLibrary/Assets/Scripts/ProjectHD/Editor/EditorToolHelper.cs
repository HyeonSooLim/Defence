using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ProjectHD.Editor
{
    public class EditorToolHelper
    {
        #region Array/List Meta 자동생성

        static string NAMESPACE = "ProjectHD.Data";

        public static void CreateArrayListMeta()
        {
            // 분석할 C# 파일 경로
            string filePath = Application.dataPath + "/Scripts/ProjectHD/Data/BasicData.cs";
            string arrayMetaFilePath = Application.dataPath + "/Scripts/ProjectHD/Data/BasicData.ArrayMeta.cs";
            string listMetaFilePath = Application.dataPath + "/Scripts/ProjectHD/Data/BasicData.ListMeta.cs";

            // C# 파일을 읽어서 클래스 이름 추출
            List<string> classOrRecordNames = new List<string>();
            classOrRecordNames.AddRange(GetClassNames(filePath));

            CreateArrayMeta(arrayMetaFilePath, classOrRecordNames);
            CreateListMeta(listMetaFilePath, classOrRecordNames);
        }

        private static void CreateArrayMeta(string arrayMetaFilePath, List<string> classOrRecordNames)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(arrayMetaFilePath))
                {
                    sw.WriteLine("//AutoScript");
                    sw.WriteLine("//수정하지마세요. ㅇㅅㅇb");
                    sw.WriteLine("//하진태_클라이언트_제작. ㅇㅅㅇb");
                    sw.WriteLine("using System.Collections.Generic;");
                    sw.WriteLine("using MessagePack;");
                    sw.WriteLine();
                    sw.WriteLine($"namespace {NAMESPACE}");
                    sw.WriteLine("{");
                    sw.WriteLine("    [MessagePackObject(true)]");
                    sw.WriteLine("    public class ArrayMeta");
                    sw.WriteLine("    {");

                    foreach (string className in classOrRecordNames)
                    {
                        if (ClassExists(className))
                        {
                            sw.WriteLine($"        public {className}[] {className} {{ get; }}");
                        }
                    }

                    sw.WriteLine("    }");
                    sw.WriteLine("}");
                }

                Debug.Log("arraymeta.cs 파일이 생성되었습니다.");
            }
            catch (Exception e)
            {
                Debug.LogError($"파일 생성 오류: {e.Message}");
            }
        }

        private static void CreateListMeta(string listMetaFilePath, List<string> classOrRecordNames)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(listMetaFilePath))
                {
                    sw.WriteLine("//AutoScript");
                    sw.WriteLine("//수정하지마세요. ㅇㅅㅇb");
                    sw.WriteLine("//하진태_클라이언트_제작. ㅇㅅㅇb");
                    sw.WriteLine("using System.Collections.Generic;");
                    sw.WriteLine("using MessagePack;");
                    sw.WriteLine();
                    sw.WriteLine($"namespace {NAMESPACE}");
                    sw.WriteLine("{");
                    sw.WriteLine("    [MessagePackObject(true)]");
                    sw.WriteLine("    public class ListMeta");
                    sw.WriteLine("    {");

                    foreach (string className in classOrRecordNames)
                    {
                        if (ClassExists(className))
                        {
                            sw.WriteLine($"         public List<{className}> {className} {{ get; }}");
                        }
                    }

                    sw.WriteLine("    }");
                    sw.WriteLine("}");
                }

                Debug.Log("listmeta.cs 파일이 생성되었습니다.");
            }
            catch (Exception e)
            {
                Debug.LogError($"파일 생성 오류: {e.Message}");
            }
        }

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

        #endregion

        #region Utility

        private static List<string> GetAllFilesInFolder(string folderPath, SearchOption searchOption)
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

        private async UniTask CleanUpMemoryAsync()
        {
            await Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        #endregion
    }
}