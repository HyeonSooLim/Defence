using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectHD
{
    /// <summary>
    /// This class is a placeholder for string extension methods.
    /// </summary>
    public static class ProjectExtensions
    {
        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        public static string UpperFirstChar(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            return char.ToUpper(input[0]) + input.Substring(1);
        }

        public static float GetTaskProgress(this UniTask task)
        {
            var field = typeof(UniTask).GetField("status", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                var status = (UniTaskStatus)field.GetValue(task);
                return status switch
                {
                    UniTaskStatus.Pending => 0f,
                    UniTaskStatus.Succeeded => 1f,
                    UniTaskStatus.Faulted => 1f,
                    UniTaskStatus.Canceled => 1f,
                    _ => 0f,
                };
            }
            return 0f;
        }
    }
}

