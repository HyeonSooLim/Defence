using MessagePack;
using ProjectHD.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ExcelTable = System.Collections.Generic.IEnumerable<System.Collections.Generic.IDictionary<string, object>>;

namespace ProjectHD.Editor
{
    public partial class EditorGenerateCode
    {
        public interface ITableImporter
        {
            public bool ImportTable(string tableName, ExcelTable table, ref List<string> addedPath);
        }

        public interface ITableConverter<E>
        {
            public E[] ConvertTable(ExcelTable table);
        }

        public class GenericImporter<E> : ITableImporter, ITableConverter<E>
        {
            public delegate bool DelegateConvertTable(ExcelTable table, out E[] data);
            private string _overrideTableName;
            private System.Func<ExcelTable, E[]> _funcConvertTable;
            private DelegateConvertTable _tryFuncConvertTable;

            public E[] ConvertTable(ExcelTable table)
            {
                if (_funcConvertTable != null)
                {
                    return _funcConvertTable.Invoke(table);
                }
                else if (_tryFuncConvertTable != null)
                {
                    return _tryFuncConvertTable.Invoke(table, out E[] data) ? data : System.Array.Empty<E>();
                }

                throw new Exception($"No Convert Function <{typeof(E)}>");
            }

            public GenericImporter(string overrideTableName, DelegateConvertTable tryFuncConvertTable)
            {
                _overrideTableName = overrideTableName;
                _tryFuncConvertTable = tryFuncConvertTable;
            }

            public GenericImporter(DelegateConvertTable tryFuncConvertTable)
            {
                _overrideTableName = string.Empty;
                _tryFuncConvertTable = tryFuncConvertTable;
            }

            public bool ImportTable(string tableName, ExcelTable table, ref List<string> addedPath)
            {
                var data = ConvertTable(table);
                var isNewData = false;
                if (string.IsNullOrEmpty(_overrideTableName))
                    isNewData |= CreateDataFile(tableName, data, ref addedPath);
                else
                    isNewData |= CreateDataFile(_overrideTableName, data, ref addedPath);
                return isNewData;
            }

            public static bool CreateDataFile<E>(string tableName, E[] data, ref List<string> addedPath)
            {
                //messagepack bytes
                var dataBytes = MessagePackSerializer.Serialize(data);
                var isNewData = false;
                isNewData |= CreateFile(tableName, IntegratedToolWindow._integratedToolSetting.BinaryOutputFolder, dataBytes, ref addedPath);

                //messagepack json
                var dataJson = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
                isNewData |= CreateFile(tableName, IntegratedToolWindow._integratedToolSetting.JsonOutputFolder, dataJson, ref addedPath);

                return isNewData;
            }

            public static bool CreateFile(string tableName, string outputPath, byte[] bytes, ref List<string> addedPath)
            {
                string fileName = tableName + ".bytes";
                string fullPath = outputPath + "/" + fileName;
                FileInfo dic = new FileInfo(fullPath);
                bool isExistFile = dic.Exists;
                if (false == dic.Directory.Exists)
                {
                    dic.Directory.Create();
                }
                System.IO.File.WriteAllBytes(fullPath, bytes);
                if (isExistFile)
                {
                    Debug.Log($"Edited Binary Data: {tableName}\nFilePath: {fullPath}");
                }
                else
                {
                    Debug.Log($"Created Binary Data: {tableName}\nFilePath: {fullPath}");
                    addedPath.Add(fullPath);
                }

                return !isExistFile;
            }

            public static bool CreateFile(string tableName, string outputPath, string json, ref List<string> addedPath)
            {
                //messagepack json
                string fileName = tableName + ".json";
                string fullPath = outputPath + "/" + fileName;
                FileInfo dic = new FileInfo(fullPath);
                bool isExistFile = dic.Exists;
                if (false == dic.Directory.Exists)
                {
                    dic.Directory.Create();
                }
                System.IO.File.WriteAllText(fullPath, json);
                if (isExistFile)
                {
                    Debug.Log($"Edited Json Data: {tableName}\nFilePath: {fullPath}");
                }
                else
                {
                    Debug.Log($"Created Json Data: {tableName}\nFilePath: {fullPath}");
                    addedPath.Add(fullPath);
                }

                return !isExistFile;
            }
        }
    }
}
