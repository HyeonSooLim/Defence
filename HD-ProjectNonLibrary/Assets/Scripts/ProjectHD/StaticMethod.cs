using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectHD
{
    public static class StaticMethod
    {
        public static DateTime ConvertDateTimeFromUnixTime(long timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(timestamp);
        }

        public static DateTime ConvertKRDateTimeFromUnixTime(long timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 9, 0, 0, 0);
            return origin.AddSeconds(timestamp);
        }

        public static bool IsFlagSet(long bitmask, long flag)
        {
            return (bitmask & flag) != 0;
        }

        // 비트마스크에 특정 플래그를 추가하는 함수
        public static long SetFlag(long bitmask, long flag)
        {
            return bitmask | flag;
        }

        // 비트마스크에서 특정 플래그를 제거하는 함수
        public static long UnsetFlag(long bitmask, long flag)
        {
            return bitmask & ~flag;
        }

        //서버에서 아이템개수를 int형으로 주고있기 때문에 아이템은 int형 기준으로는 2147M까지만 표현되고있다. 2024.05.29
        public static string LongValueMark(long value)
        {
            var res = string.Empty;
            var maxLength = 6;
            var max = (int)Math.Pow(10, maxLength);

            switch (value / max)
            {
                case < 1:
                    res = value.ToString();
                    break;
                case < 1000:
                    value = value / 1000;
                    res = value + "K";
                    break;
                case < 1000000:
                    value = value / 1000000;
                    res = value + "M";
                    break;
                case < 1000000000:
                    value = value / 1000000000;
                    res = value + "B";
                    break;
            }

            return res;
        }

        static readonly string[] suffixes = { "Bytes", "KB", "MB", "GB", "TB", "PB" };
        public static string FormatSize(System.Int64 bytes)
        {
            int counter = 0;
            decimal number = (decimal)bytes;
            while (System.Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return $"{number:n1}{suffixes[counter]}";
        }
        /// <summary>
        /// 0 = Bytes, 1 = KB, 2 = MB, 3 = GB, 4 = TB, 5 = PB
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        public static string FormatSize(System.Int64 bytes, int limit)
        {
            int counter = 0;
            decimal number = (decimal)bytes;
            while (System.Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
                if (counter >= limit) break;
            }
            return $"{number:n1}{suffixes[counter]}";
        }

        public static string FormatSizeCeiling(System.Int64 bytes, int Digit, int limit)
        {
            int counter = 0;
            decimal number = (decimal)bytes;
            while (System.Math.Round(number / 1024) >= 1)
            {
                number = Math.Round(number / 1024);
                counter++;
                if (counter >= limit) break;
            }

            var ceilingNumber = BytesCeiling((int)number, Digit);
            return $"{ceilingNumber}{suffixes[counter]}";
        }

        public static double BytesCeiling(int Value, int Digit)
        {
            var calculateValue = Math.Pow(10.0, Digit);
            double Temp = Value / calculateValue;
            if (Temp < 1)
            {
                return Value;
            }
            return Math.Ceiling(Temp) * calculateValue;
        }

        public static string FormatNumber(ulong number)
        {
            if (number >= 1_000_000_000)
                return $"{number / 1_000_000_000.0:0.##}B"; // 십억 단위
            else if (number >= 1_000_000)
                return $"{number / 1_000_000.0:0.##}M"; // 백만 단위
            else if (number >= 1_000)
                return $"{number / 1_000.0:0.##}K"; // 천 단위
            else
                return number.ToString(); // 천 단위 미만
        }

        public static T GetRandomElement<T>(T[] array)
        {
            if (array == null || array.Length == 0)
                throw new ArgumentException("Array cannot be null or empty");
            int index = UnityEngine.Random.Range(0, array.Length);
            return array[index];
        }

        public static T GetRandomElement<T>(IReadOnlyCollection<T> iCollection)
        {
            if (iCollection == null || iCollection.Count == 0)
                throw new ArgumentException("Array cannot be null or empty");
            int index = UnityEngine.Random.Range(0, iCollection.Count);
            var enumerator = iCollection.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (index-- == 0)
                    return enumerator.Current;
            }
            throw new InvalidOperationException("Should never reach here");
        }

        #region Hex

        /// <summary>
        /// 2칸 이내라는 조건이라면 HexDistance <= 2
        /// </summary>
        /// <param name="q1">자신의 가로 방향</param>
        /// <param name="r1">자신의 대각 방향</param>
        /// <param name="q2">타겟의 가로 방향</param>
        /// <param name="r2">타겟의 대각 방향</param>
        /// <returns></returns>
        public static int HexDistance(int q1, int r1, int q2, int r2)
        {
            return (Mathf.Abs(q1 - q2)
                  + Mathf.Abs(q1 + r1 - q2 - r2)
                  + Mathf.Abs(r1 - r2)) / 2;
        }

        public static Vector3 HexToWorld(int q, int r, float hexSize)
        {
            float x = hexSize * 3f / 2f * q;
            float z = hexSize * Mathf.Sqrt(3f) * (r + q / 2f);
            return new Vector3(x, 0f, z); // Y축은 지면 기준
        }

        public static Vector2Int WorldToHex(float hexWidth, float hexHeight, Vector3 position, Vector3 hexOffset)
        {
            position -= hexOffset; // 헥스 그리드 이동 보정(좌표기준 0,0,0 맞추기 위함)
            float q = position.x / (hexWidth * 0.75f); // Flat-top 기준 q 계산
            float r = (position.z - (q * hexHeight / 2f)) / hexHeight; // r 계산 보정
            return HexRound(q, r);
        }

        public static Vector2Int HexRound(float q, float r)
        {
            float x = q;
            float z = r;
            float y = -x - z;

            int rx = Mathf.RoundToInt(x);
            int ry = Mathf.RoundToInt(y);
            int rz = Mathf.RoundToInt(z);

            float x_diff = Mathf.Abs(rx - x);
            float y_diff = Mathf.Abs(ry - y);
            float z_diff = Mathf.Abs(rz - z);

            if (x_diff > y_diff && x_diff > z_diff)
                rx = -ry - rz;
            else if (y_diff > z_diff)
                ry = -rx - rz;
            else
                rz = -rx - ry;

            return new Vector2Int(rx, rz); // 반환되는 값이 (q, r)
        }

        #endregion

        public static Vector2 WorldPositionToUIAnchoredPosition(Vector3 worldPosition, RectTransform canvasRectTransform, Camera canvasCamera)
        {
            var screenPoint = CameraManager.Instance.MainCamera.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,
                screenPoint,
                canvasCamera,
                out Vector2 anchoredPosition
            );

            return anchoredPosition;
        }
    }
}