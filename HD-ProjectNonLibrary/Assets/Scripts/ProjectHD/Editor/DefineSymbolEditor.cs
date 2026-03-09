using System.Text.RegularExpressions;
using UnityEditor;

public static class DefineSymbolEditor
{
    public struct DefineSymbolData
    {
        public BuildTargetGroup BuildTargetGroup; // 현재 빌드 타겟 그룹
        public string FullSymbolString;           // 현재 빌드 타겟 그룹에서 정의된 심볼 문자열 전체
        public Regex SymbolRegex;

        public DefineSymbolData(string symbol)
        {
            BuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            FullSymbolString = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup);
            SymbolRegex = new Regex(@"\b" + symbol + @"\b(;|$)");
        }
    }

    /// <summary> 심볼이 이미 정의되어 있는지 검사 </summary>
    public static bool IsSymbolAlreadyDefined(string symbol)
    {
        DefineSymbolData dsd = new DefineSymbolData(symbol);

        return dsd.SymbolRegex.IsMatch(dsd.FullSymbolString);
    }

    /// <summary> 심볼이 이미 정의되어 있는지 검사 </summary>
    public static bool IsSymbolAlreadyDefined(string symbol, out DefineSymbolData dsd)
    {
        dsd = new DefineSymbolData(symbol);

        return dsd.SymbolRegex.IsMatch(dsd.FullSymbolString);
    }

    /// <summary> 특정 디파인 심볼 추가 </summary>
    public static void AddDefineSymbol(string symbol)
    {
        // 기존에 존재하지 않으면 끝에 추가
        if (!IsSymbolAlreadyDefined(symbol, out var dsd))
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(dsd.BuildTargetGroup, $"{dsd.FullSymbolString};{symbol}");
        }
    }

    /// <summary> 특정 디파인 심볼 제거 </summary>
    public static void RemoveDefineSymbol(string symbol)
    {
        // 기존에 존재하면 제거
        if (IsSymbolAlreadyDefined(symbol, out var dsd))
        {
            string strResult = dsd.SymbolRegex.Replace(dsd.FullSymbolString, "");

            PlayerSettings.SetScriptingDefineSymbolsForGroup(dsd.BuildTargetGroup, strResult);
        }
    }
}