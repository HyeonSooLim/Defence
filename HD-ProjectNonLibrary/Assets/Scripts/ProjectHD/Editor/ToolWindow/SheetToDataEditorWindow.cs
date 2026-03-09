using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MessagePack;
using UnityEditor;
using UnityEngine;
using ExcelTable = System.Collections.Generic.IEnumerable<System.Collections.Generic.IDictionary<string, object>>;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using MiniExcelLibs;
using Cysharp.Threading.Tasks;

namespace ProjectHD.Editor
{
    public class SheetToDataEditorWindow : EditorWindow
    {
        [MenuItem("Tools/ProjectHD/SheetToJson")]
        public static void ShowEditor()
        {
            var wnd = GetWindow<SheetToDataEditorWindow>();
            wnd.titleContent = new GUIContent("SheetToDataEditorWindow");
        }

        private static char[] spliter = new[] { ' ', ',', ';', '.', '|' };

        //[TitleGroup("입력 시트 파일"), Sirenix.OdinInspector.FilePath, SerializeField]
        //public string[] SheetAssetPathList;
        public string[] SheetAssetPathList = new string[1];

        public static readonly string BinaryOutputFolder = "Assets/GameResources/MessagePackBinary";
        public static readonly string JsonOutputFolder = "Assets/GameResources/MessagePackJson";

        private Vector2 scrollPos;

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            GUILayout.Label("입력 시트 파일", EditorStyles.boldLabel);

            int newSize = EditorGUILayout.IntField("Sheet Path Count", SheetAssetPathList.Length);
            if (newSize != SheetAssetPathList.Length)
            {
                System.Array.Resize(ref SheetAssetPathList, newSize);
            }

            for (int i = 0; i < SheetAssetPathList.Length; i++)
            {
                SheetAssetPathList[i] = EditorGUILayout.TextField($"Sheet Path {i + 1}", SheetAssetPathList[i]);
            }

            GUILayout.Space(10);
            GUILayout.Label("출력", EditorStyles.boldLabel);

            if (GUILayout.Button("Sheet To Data", GUILayout.Height(40)))
            {
                SheetToData();
            }

            GUILayout.Space(10);
            GUILayout.Label("코드 자동화 (신규 시트 추가시 MessagePack Code Generation 이전에 한번 눌러주세요)", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Array/List Meta", GUILayout.Height(40)))
            {
                CreateArrayListMeta();
            }

            GUILayout.Space(10);
            GUILayout.Label("Editor Test)", EditorStyles.boldLabel);

            if (GUILayout.Button("Test MessagePackLoad", GUILayout.Height(40)))
            {
                TestMessagePackLoad();
            }

            GUILayout.Space(10);
            GUILayout.Label("Editor Test", EditorStyles.boldLabel);

            if (GUILayout.Button("Test JSONLoad", GUILayout.Height(40)))
            {
                TestJsonLoad();
            }

            EditorGUILayout.EndScrollView();
        }


        //[TitleGroup("출력"), Button(ButtonSizes.Large)]
        public void SheetToData()
        {
            CacheTable();
            TableToData();
            UncacheTable();

            AssetDatabase.SaveAssets();
        }

        private static Dictionary<string, ExcelTable> _cachedTable = new();
        private void CacheTable()
        {
            _cachedTable.Clear();
            foreach (var sheetAssetPath in SheetAssetPathList)
            {
                if (SheetImportFromPath(sheetAssetPath))
                {
                    return;
                }
            }
        }

        private static bool SheetImportFromPath(string sheetAssetPath)
        {
            if (string.IsNullOrEmpty(sheetAssetPath)) return true;
            if (string.IsNullOrWhiteSpace(sheetAssetPath)) return true;

            var tableNames = MiniExcel.GetSheetNames(sheetAssetPath);
            foreach (var tableName in tableNames)
            {
                var rawTable = MiniExcel.Query(sheetAssetPath, sheetName: tableName, useHeaderRow: true).ToList();

                var table = rawTable
                    .Select(row => row as IDictionary<string, object>)
                    .Where(row => row != null)
                    .ToList();

                _cachedTable.Add(tableName, table);
            }

            return false;
        }

        public static bool SheetImport(string path)
        {
            _cachedTable.Clear();
            SheetImportFromPath(path);
            TableToData();
            UncacheTable();

            AssetDatabase.SaveAssets();
            return true;
        }

        private static void UncacheTable()
        {
            _cachedTable.Clear();
        }

        //[TitleGroup("코드 자동화 (신규 시트 추가시 MessagePack Code Generation 이전에 한번 눌러주세요)"), Button(ButtonSizes.Large)]
        private void CreateArrayListMeta()
        {
            EditorToolHelper.CreateArrayListMeta();
        }

        private static void TableToData()
        {
            Dictionary<string, ITableImporter> dicTableImporter = new();          
            // 자동 등록되지 않는 예외 케이스는 수동 추가 (데이터 클래스 이름과 엑셀 시트 이름이 다른 경우) (CheckKeyAndClassName 함수로 체크 가능)
            // 예:
            //dicTableImporter.Add("Item", new GenericImporter<Data.GameItem>("GameItem", TryConvertTableToData));

            dicTableImporter.AddRange(CreateTableImporterDictionary());
            dicTableImporter.Remove("Dummy"); // Dummy는 임포트하지 않음
            dicTableImporter.Remove("ImmutableDummy"); // ImmutableDummy 임포트하지 않음
            dicTableImporter.Remove("DummyDummy"); // DummyDummy 임포트하지 않음


#if UNITY_EDITOR
            Utilities.InternalDebug.Log($"테이블 임포터 생성 완료 개수(임포터/캐시테이블): {dicTableImporter.Count}/{_cachedTable.Count}");
#endif
            foreach (var (tableName, table) in _cachedTable)
            {
                if (!dicTableImporter.TryGetValue(tableName, out var importer)) continue;
                importer.ImportTable(tableName, table);
            }
        }

        private static Dictionary<string, ITableImporter> CreateTableImporterDictionary()
        {
            var dicTableImporter = new Dictionary<string, ITableImporter>();

            // ProjectHD.Data 네임스페이스에 속하는 타입들을 가져옵니다.
            var namespaceString = "ProjectHD.Data";
            var assembly = typeof(Data.Dummy).Assembly;
            var dataTypes = assembly.GetTypes()
                .Where(t => (t.IsClass || t.IsValueType)
                            && t.Namespace != null
                            && t.Namespace.StartsWith(namespaceString)
                            && !t.IsAbstract
                            && !t.IsGenericType
                            && t.GetCustomAttribute<MessagePackObjectAttribute>()?.KeyAsPropertyName == true // 조건 추가
                            && t.GetCustomAttribute<MasterMemory.MemoryTableAttribute>() != null);

            foreach (var type in dataTypes)
            {
                var methodName = "TryConvertTableToData";
                var expectedOutType = type.MakeArrayType(); // 예: Data.TutorialDefine[]

                // 변환 메서드의 시그니처는 (ExcelTable table, out T[] data)
                // 테이블 메서드가 있는 클래스 참조
                var method = typeof(GeneratedTableImporters).GetMethod(
                    methodName,
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new Type[] { typeof(ExcelTable), expectedOutType.MakeByRefType() },
                    null
                );

                if (method == null)
                {
                    Utilities.InternalDebug.Log($"변환 메서드 {methodName}을(를) {type.Name} 타입에 대해 찾지 못했습니다.");
                    continue;
                }

                // GenericImporter<T>의 닫힌 제네릭 타입 생성
                var importerType = typeof(GenericImporter<>).MakeGenericType(type);

                // 해당 클래스 내부에 정의된 delegate 타입 (DelegateConvertTable) 획득
                var delegateType = importerType.GetNestedType("DelegateConvertTable");
                if (delegateType == null)
                {
                    Utilities.InternalDebug.LogWarning($"DelegateConvertTable 타입을 {importerType.Name}에서 찾지 못했습니다.");
                    continue;
                }

                // delegateType이 아직 generic(열린 타입)이라면 이를 닫힌 타입으로 바꿉니다.
                if (delegateType.ContainsGenericParameters)
                {
                    // 여기서는 importerType은 이미 GenericImporter<T>로 닫혀 있으므로,
                    // delegateType의 generic 매개변수를 importerType의 generic 인수들로 채워줍니다.
                    var typeArguments = importerType.GetGenericArguments();
                    delegateType = delegateType.MakeGenericType(typeArguments);
                }

                // 아래는 기존의 wrapper 헬퍼(DelegateWrapperHelper<T>)를 사용한 delegate 생성
                // 대신 Expression을 사용하여 delegate를 생성하도록 합니다.
                var helperType = typeof(DelegateWrapperHelper<>).MakeGenericType(type);
                // helper instance 생성: 대상 메서드를 전달하여 초기화합니다.
                var helperInstance = Activator.CreateInstance(helperType, method);
                // helper의 Wrapper 메서드 (public bool Wrapper(ExcelTable, out T[]))
                var wrapperMethod = helperType.GetMethod("Wrapper", BindingFlags.Public | BindingFlags.Instance);
                if (wrapperMethod == null)
                {
                    Utilities.InternalDebug.LogWarning($"Wrapper 메서드를 {helperType.Name}에서 찾지 못했습니다.");
                    continue;
                }

                try
                {
                    // Expression lambda를 만들어 delegate를 생성합니다.
                    // delegateType의 Invoke 시그니처는: bool Invoke(ExcelTable table, out T[] data)
                    // 파라미터 정의:
                    var paramTable = System.Linq.Expressions.Expression.Parameter(typeof(ExcelTable), "table");
                    var paramData = System.Linq.Expressions.Expression.Parameter(expectedOutType.MakeByRefType(), "data");  // out parameter

                    // helperInstance를 상수로 캡처합니다.
                    var helperInstanceExp = System.Linq.Expressions.Expression.Constant(helperInstance, helperType);
                    // 호출 표현식을 생성: helperInstance.Wrapper(table, out data)
                    var callExp = System.Linq.Expressions.Expression.Call(helperInstanceExp, wrapperMethod, paramTable, paramData);

                    // lambda 생성: (table, out data) => helperInstance.Wrapper(table, out data)
                    var lambda = System.Linq.Expressions.Expression.Lambda(delegateType, callExp, paramTable, paramData);
                    var methodDelegate = lambda.Compile();

                    // GenericImporter<T> 인스턴스 생성 (delegate 생성자를 사용)
                    var importer = Activator.CreateInstance(importerType, methodDelegate);
                    dicTableImporter.Add(type.Name, (ITableImporter)importer);
                }
                catch (Exception ex)
                {
                    Utilities.InternalDebug.LogError($"Delegate 생성 실패 (식 트리): {ex.Message}");
                    continue;
                }
            }

            return dicTableImporter;
        }

        // (예: SheetToDataEditorWindow 또는 별도 유틸 클래스 내에 추가)
        private class DelegateWrapperHelper<T>
        {
            private readonly MethodInfo _targetMethod;

            public DelegateWrapperHelper(MethodInfo targetMethod)
            {
                _targetMethod = targetMethod;
            }

            // 이 메서드의 시그니처는 GenericImporter<T>.DelegateConvertTable과 일치해야 합니다.
            // 즉, ExcelTable (using ExcelTable = IEnumerable<IDictionary<string,object>>)와 out T[]를 받습니다.
            public bool Wrapper(ExcelTable table, out T[] data)
            {
                data = null;
                // 대상 메서드가 실제로는 IEnumerable<IDictionary<string, object>>를 받는다고 가정합니다.
                // ExcelTable와 IEnumerable<IDictionary<string, object>>는 동일 타입입니다.
                object[] parameters = new object[] { table, null };

                // 대상 메서드 호출 (정적 메서드이므로 첫 인자는 null)
                bool success = (bool)_targetMethod.Invoke(null, parameters);
                data = (T[])parameters[1];

                return success;
            }
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
                    sw.WriteLine("namespace DigimonSC.Data");
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

        public static bool ClassExists(string className)
        {
            try
            {
                var assemblyName = "Assembly-CSharp";
                var assembly = Assembly.Load(assemblyName);

                if (assembly.GetType("DigimonSC.Data." + className) == null)
                    return false;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        //[TitleGroup("Editor Test"), Button(ButtonSizes.Medium)]
        private void TestMessagePackLoad()
        {
            if (Application.isPlaying) return;

            DataManager dataManager = new();
            dataManager.LoadAllMessagePackAsync().Forget();

        }

        //[TitleGroup("Editor Test"), Button(ButtonSizes.Medium)]
        private void TestJsonLoad()
        {
            if (Application.isPlaying) return;

            DataManager dataManager = new();
            dataManager.LoadAllJsonsAsync().Forget();
        }

        #region

        public interface ITableImporter
        {
            public void ImportTable(string tableName, ExcelTable table);
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

            public GenericImporter(string overrideTableName, System.Func<ExcelTable, E[]> funcConvertTable)
            {
                _overrideTableName = overrideTableName;
                _funcConvertTable = funcConvertTable;
            }

            public GenericImporter(System.Func<ExcelTable, E[]> funcConvertTable)
            {
                _overrideTableName = string.Empty;
                _funcConvertTable = funcConvertTable;
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

            public void ImportTable(string tableName, ExcelTable table)
            {
                var data = ConvertTable(table);
                if (string.IsNullOrEmpty(_overrideTableName))
                    CreateDataFile(tableName, data);
                else
                    CreateDataFile(_overrideTableName, data);
            }

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

            public bool TryConvertTable(ExcelTable table, out E[] data)
            {
                data = null;
                return _tryFuncConvertTable?.Invoke(table, out data) ?? false;
            }
        }

        #endregion

        public static void CreateDataFile<E>(string tableName,
            IEnumerable<IDictionary<string, object>> table,
            Func<IEnumerable<IDictionary<string, object>>, E[]> convertFunc)
        {
            var data = convertFunc(table);

            //messagepack bytes
            var dataBytes = MessagePackSerializer.Serialize(data);
            CreateFile(tableName, dataBytes);

            //messagepack json
            var dataJson = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
            CreateFile(tableName, dataJson);
        }

        public static void CreateDataFile<E>(string tableName, E[] data)
        {
            //messagepack bytes
            var dataBytes = MessagePackSerializer.Serialize(data);
            CreateFile(tableName, dataBytes);

            //messagepack json
            var dataJson = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
            CreateFile(tableName, dataJson);
        }

        public static void CreateFile(string tableName, string outputPath, byte[] bytes)
        {
            //messagepack bytes
            if (!string.IsNullOrEmpty(BinaryOutputFolder))
            {
                outputPath = "./" + outputPath;
                Directory.CreateDirectory(outputPath);
                string fileName = tableName + ".bytes";
                using BinaryWriter binaryWriter =
                    new BinaryWriter(File.Open(outputPath + "/" + fileName, FileMode.OpenOrCreate));
                binaryWriter.Write(bytes);

                Debug.Log("Created Binary Data: " + tableName);
            }
        }

        public static void CreateFile(string tableName, byte[] bytes)
        {
            CreateFile(tableName, BinaryOutputFolder, bytes);
        }

        public static void CreateFile(string tableName, string outputPath, string json)
        {
            //messagepack json
            if (!string.IsNullOrEmpty(JsonOutputFolder))
            {
                outputPath = "./" + outputPath;
                Directory.CreateDirectory(outputPath);
                string fileName = tableName + ".json";
                using StreamWriter strmWriter =
                    new StreamWriter(outputPath + "/" + fileName, false, System.Text.Encoding.UTF8);
                strmWriter.Write(json);

                Debug.Log("Created Json Data: " + tableName);
            }
        }

        public static void CreateFile(string tableName, string json)
        {
            CreateFile(tableName, JsonOutputFolder, json);
        }

        public static UnityEngine.Vector3 ParseVector3(string input)
        {
            // 정규 표현식을 사용하여 숫자 추출
            var match = Regex.Match(input, @"Vector3\s*\(([^,]+),([^,]+),([^)]+)\)");
            if (!match.Success)
            {
                throw new ArgumentException("올바른 Vector3 형식이 아닙니다.");
            }

            try
            {
                float x = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                float y = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                float z = float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

                return new Vector3(x, y, z);
            }
            catch (FormatException)
            {
                throw new ArgumentException("숫자 변환에 실패했습니다.");
            }
        }

        #region Dummy

        public static Data.Dummy[] ConvertTableToDummy(IEnumerable<IDictionary<string, object>> table)
        {
            List<Data.Dummy> listData = new List<Data.Dummy>();

            foreach (var row in table)
            {
                int seed = 0;
                string value = string.Empty;
                foreach (var kvp in row)
                {
                    string header = kvp.Key;
                    if (string.IsNullOrEmpty(header)) continue;
                    if (string.IsNullOrWhiteSpace(header)) continue;
                    var numericValue = kvp.Value is double ? Convert.ToDouble(kvp.Value) : 0;
                    var boolValue = kvp.Value is bool && Convert.ToBoolean(kvp.Value);
                    var stringValue = kvp.Value is string ? Convert.ToString(kvp.Value) : string.Empty;
                    var dateValue = kvp.Value is DateTime ? kvp.Value : default;

                    switch (header)
                    {
                        case "Seed":
                            seed = (int)numericValue;
                            break;
                        case "Value":
                            value = stringValue;
                            break;
                    }
                }

                if (seed == 0) continue;

                listData.Add(new Data.Dummy(seed, value));
            }

            return listData.OrderBy(r => r.Seed).ToArray();
        }

        public static bool TryConvertTableToData(IEnumerable<IDictionary<string, object>> table, out Data.Dummy[] data)
        {
            data = ConvertTableToDummy(table);
            return data != null;
        }

        #endregion

        #region Immutable Dummy

        public static Data.ImmutableDummy[] ConvertTableToImmutableDummy(IEnumerable<IDictionary<string, object>> table)
        {
            List<Data.ImmutableDummy> listData = new List<Data.ImmutableDummy>();

            foreach (var row in table)
            {
                int seed = 0;
                string value = string.Empty;
                foreach (var kvp in row)
                {
                    string header = kvp.Key;
                    if (string.IsNullOrEmpty(header)) continue;
                    if (string.IsNullOrWhiteSpace(header)) continue;
                    var numericValue = kvp.Value is double ? Convert.ToDouble(kvp.Value) : 0;
                    var boolValue = kvp.Value is bool && Convert.ToBoolean(kvp.Value);
                    var stringValue = kvp.Value is string ? Convert.ToString(kvp.Value) : string.Empty;
                    var dateValue = kvp.Value is DateTime ? kvp.Value : default;

                    switch (header)
                    {
                        case "Seed":
                            seed = (int)numericValue;
                            break;
                        case "Value":
                            value = stringValue;
                            break;
                    }
                }

                if (seed == 0) continue;

                listData.Add(new Data.ImmutableDummy(seed, value));
            }

            return listData.OrderBy(r => r.Seed).ToArray();
        }

        public static bool TryConvertTableToData(IEnumerable<IDictionary<string, object>> table, out Data.ImmutableDummy[] data)
        {
            data = ConvertTableToImmutableDummy(table);
            return data != null;
        }

        #endregion

        #region Dummy Dummy

        public static Data.DummyDummy[] ConvertTableToDummyDummy(IEnumerable<IDictionary<string, object>> table)
        {
            List<Data.DummyDummy> listData = new List<Data.DummyDummy>();

            foreach (var row in table)
            {
                int seed = 0;
                int seed2 = 0;
                string value = string.Empty;
                foreach (var kvp in row)
                {
                    string header = kvp.Key;
                    if (string.IsNullOrEmpty(header)) continue;
                    if (string.IsNullOrWhiteSpace(header)) continue;
                    var numericValue = kvp.Value is double ? Convert.ToDouble(kvp.Value) : 0;
                    var boolValue = kvp.Value is bool && Convert.ToBoolean(kvp.Value);
                    var stringValue = kvp.Value is string ? Convert.ToString(kvp.Value) : string.Empty;
                    var dateValue = kvp.Value is DateTime ? kvp.Value : default;

                    switch (header)
                    {
                        case "Seed":
                            seed = (int)numericValue;
                            break;
                        case "Seed2":
                            seed2 = (int)numericValue;
                            break;
                        case "Value":
                            value = stringValue;
                            break;
                    }
                }

                if (seed == 0) continue;

                listData.Add(new Data.DummyDummy(seed, seed2, value));
            }

            return listData.OrderBy(r => r.Seed).ToArray();
        }

        public static bool TryConvertTableToData(IEnumerable<IDictionary<string, object>> table, out Data.DummyDummy[] data)
        {
            data = ConvertTableToDummyDummy(table);
            return data != null;
        }

        #endregion
    }
}