using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System;
using System.Linq;

namespace ProjectHD
{
    public static class TableConverter
    {
        private static readonly char ARRAY_SPLIT_PATTERN = '/';
        private static readonly Dictionary<Type, ConstructorInfo> CtorCache = new();
        private static readonly Dictionary<Type, ParameterInfo[]> ParamCache = new();

        public static T[] ConvertTableTo<T>(IEnumerable<IDictionary<string, object>> table)
        {
            var ctor = GetCachedConstructor(typeof(T));
            var parameters = GetCachedParameters(typeof(T));
            var list = new List<T>();

            // PrimaryKeyAttribute 검사할 프로퍼티 목록
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var originalRow in table)
            {
                var row = new Dictionary<string, object>(originalRow, StringComparer.OrdinalIgnoreCase);

                bool isCorrectPrimaryKey = true;
                // PrimaryKey 검사
                foreach (var p in properties)
                {
                    if (p.GetCustomAttribute<MasterMemory.PrimaryKeyAttribute>() != null)
                    {
                        if (row.TryGetValue(p.Name, out var value))
                        {
                            if (!CheckPrimaryKey(value, p.PropertyType))
                            {
                                isCorrectPrimaryKey = false;
                                break;
                            }
                        }
                        else
                        {
                            isCorrectPrimaryKey = false;
                            break;
                        }
                    }
                }
                if (!isCorrectPrimaryKey)
                    continue;

                // 생성자 매개변수 매핑
                var args = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    var p = parameters[i];
                    args[i] = IsSimpleType(p.ParameterType)
                        ? GetValueFromRow(row, p.Name, p.ParameterType)
                        : ConvertComplexType(p.ParameterType, row, p.Name);
                }

                list.Add((T)ctor.Invoke(args));
            }

            return list.ToArray();
        }

        private static object ConvertComplexType(Type targetType, IDictionary<string, object> row, string prefix)
        {
            var ctor = GetCachedConstructor(targetType);
            var parameters = GetCachedParameters(targetType);
            var args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                var fullName = $"{prefix}.{p.Name}";
                args[i] = IsSimpleType(p.ParameterType)
                    ? GetValueFromRow(row, fullName, p.ParameterType)
                    : ConvertComplexType(p.ParameterType, row, fullName);
            }
            return ctor.Invoke(args);
        }

        private static object GetValueFromRow(IDictionary<string, object> row, string colName, Type targetType)
        {
            if (row.TryGetValue(colName, out var value))
                return ConvertValue(value, targetType);
            return GetDefault(targetType);
        }

        private static bool IsSimpleType(Type type) =>
            type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) ||
            type == typeof(DateTime) || type == typeof(int[]) || type == typeof(long[]) || type == typeof(UnityEngine.Vector3);

        public static object ConvertValue(object value, Type targetType)
        {
            if (value == null || targetType == null)
                return GetDefault(targetType);

            // Nullable 처리
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
                return ConvertValue(value, underlyingType);

            string strValue = value?.ToString()?.Trim() ?? string.Empty;

            // "null" 문자열 전처리
            if (string.Equals(strValue, "null", StringComparison.OrdinalIgnoreCase))
                strValue = string.Empty;

            try
            {
                // 배열 처리
                if (targetType.IsArray)
                {
                    var elementType = targetType.GetElementType();
                    if (value is string s)
                    {
                        var tokens = s.Split(ARRAY_SPLIT_PATTERN, StringSplitOptions.RemoveEmptyEntries);
                        var arr = Array.CreateInstance(elementType, tokens.Length);
                        for (int i = 0; i < tokens.Length; i++)
                        {
                            if (elementType == typeof(UnityEngine.Vector3))
                                arr.SetValue(ParseVector3OrDefault(tokens[i]), i);
                            else
                                arr.SetValue(ConvertValue(tokens[i], elementType), i);
                        }
                        return arr;
                    }
                    return Array.CreateInstance(elementType, 0);
                }

                // Vector3 처리
                if (targetType == typeof(UnityEngine.Vector3))
                    return ParseVector3OrDefault(strValue);

                // string
                if (targetType == typeof(string))
                    return strValue;

                // int
                if (targetType == typeof(int))
                {
                    if (int.TryParse(strValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) return i;
                    if (double.TryParse(strValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return (int)d;
                    return GetDefault(targetType);
                }

                // float
                if (targetType == typeof(float))
                {
                    if (float.TryParse(strValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)) return f;
                    return GetDefault(targetType);
                }

                // double
                if (targetType == typeof(double))
                {
                    if (double.TryParse(strValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return d;
                    return GetDefault(targetType);
                }

                // bool
                if (targetType == typeof(bool))
                {
                    if (bool.TryParse(strValue, out var b)) return b;
                    return strValue.ToLowerInvariant() switch
                    {
                        "1" or "yes" or "on" => true,
                        "0" or "no" or "off" => false,
                        _ => GetDefault(targetType),
                    };
                }

                // long
                if (targetType == typeof(long))
                {
                    if (long.TryParse(strValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)) return l;
                    return GetDefault(targetType);
                }

                // enum
                if (targetType.IsEnum)
                {
                    if (Enum.TryParse(targetType, strValue, true, out var enumVal))
                        return enumVal;
                    return GetDefault(targetType);
                }

                // 기타 타입
                return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch
            {
                return GetDefault(targetType);
            }
        }

        private static object GetDefault(Type type) =>
            type.IsValueType ? Activator.CreateInstance(type) :
            type == typeof(string) ? string.Empty : null;

        /// <summary>
        /// 엑셀 데이터 텍스트에서 Vector3(0,0,0) 형식의 문자열을 파싱하여 Vector3 객체로 변환합니다.
        /// CultureInfo.InvariantCulture 는 .을 소수점으로 인식하게 한다(문화권에 따라 ,를 소수점으로 인식하는 경우가 있음)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool TryParseVector3(string input, out UnityEngine.Vector3 vector3)
        {
            vector3 = default;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            // Vector3(x,y,z) 형식
            var match = Regex.Match(input, @"Vector3\s*\(([^,]+),([^,]+),([^)]+)\)");
            if (match.Success &&
                float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                float.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var y) &&
                float.TryParse(match.Groups[3].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
            {
                vector3 = new UnityEngine.Vector3(x, y, z);
                return true;
            }

            // x,y,z 형식
            var parts = input.Split(',');
            if (parts.Length == 3 &&
                float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
                float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y) &&
                float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
            {
                vector3 = new UnityEngine.Vector3(x, y, z);
                return true;
            }

            return false;
        }

        private static object ParseVector3OrDefault(string s) =>
            TryParseVector3(s, out var v3) ? v3 : default;

        private static ConstructorInfo GetCachedConstructor(Type type)
        {
            if (!CtorCache.TryGetValue(type, out var ctor))
            {
                ctor = type.GetConstructors().FirstOrDefault()
                    ?? throw new InvalidOperationException($"{type.Name}에 public 생성자가 없습니다.");
                CtorCache[type] = ctor;
            }
            return ctor;
        }

        private static ParameterInfo[] GetCachedParameters(Type type)
        {
            if (!ParamCache.TryGetValue(type, out var parameters))
            {
                parameters = GetCachedConstructor(type).GetParameters();
                ParamCache[type] = parameters;
            }
            return parameters;
        }

        private static bool CheckPrimaryKey(object value, Type targetType)
        {
            if (value == null || targetType == null)
                return false;

            // Nullable<T> 처리
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
                return CheckPrimaryKey(value, underlyingType); // ← 결과 반환

            string strValue = value?.ToString()?.Trim() ?? string.Empty;
            if (string.Equals(strValue, "null", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(strValue))
                return false;

            return true;
        }
    }
}