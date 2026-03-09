using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectHD
{
    public enum DeviceRepositoryKey
    {
        None,
        
        //Editor 전용
        Editor_LocalAccount = 100000,
        Editor_LocalAccountIPAddress,
        Editor_LocalAccountPort,
        Editor_LocalAccountUseCustomIP,
        Editor_ShowDebugConsole,
        Editor_SceneUnload,
        Editor_ResourceUnload,
        Editor_Build_Version,
        Editor_Build_BundleNumber,
        Editor_Build_DevMode,
        Editor_Build_CleanBuild,
        Editor_Build_BuildOutputPath,
        Editor_Build_BuildName,
        Editor_LocalCurrentVersionType,
        Editor_Build_AppBundle,
        Editor_Build_MarketType,
        Editor_Download_Sheet_Path,
        Editor_Build_FTP_Url,
        Editor_Build_FTP_UserName,
        Editor_Build_FTP_Password,
        Editor_Build_FTP_localPath,
        Editor_AutoAddressable_Grouping,
        Editor_AutoAddressable_Addressable,
        Editor_AutoAddressable_Label,
        Editor_Adventure_ClearAllToSelect,
        Editor_Build_FTP_Host,
        Editor_Build_FTP_Path,
        Editor_Build_FTP_PathIndex,
        Editor_AutoAddressable_Schema,
    }


    public static class DeviceRepository
    {
        private static Dictionary<DeviceRepositoryKey, string> cache = new();

        private static List<DeviceRepositoryKey> _ignoreDeleteWhenLogout = new()
        {
            DeviceRepositoryKey.Editor_LocalAccount
        };

        public static void ClearCache()
        {
            cache.Clear();
        }

        public static void ClearPlayerPrefs()
        {
            foreach (var enumItem in Enum.GetValues(typeof(DeviceRepositoryKey)))
            {
                var key = (DeviceRepositoryKey)enumItem;
                if (_ignoreDeleteWhenLogout.Contains(key))
                    continue;
                PlayerPrefs.DeleteKey(key.ToString());
            }
        }

        public static void SaveKeyForBoolean(DeviceRepositoryKey key, bool val)
        {
            if (!cache.ContainsKey(key))
                cache.Add(key, key.ToString());
            PlayerPrefs.SetInt(cache[key], val ? 1 : 0);
        }

        public static void SaveKeyForInt(DeviceRepositoryKey key, int val)
        {
            if (!cache.ContainsKey(key))
                cache.Add(key, key.ToString());
            PlayerPrefs.SetInt(cache[key], val);
        }

        public static void SaveKeyForFloat(DeviceRepositoryKey key, float val)
        {
            if (!cache.ContainsKey(key))
                cache.Add(key, key.ToString());
            PlayerPrefs.SetFloat(cache[key], val);
        }

        public static void SaveKeyForString(DeviceRepositoryKey key, string val)
        {
            if (!cache.ContainsKey(key))
                cache.Add(key, key.ToString());
            PlayerPrefs.SetString(cache[key], val);
        }

        public static void SaveKeyForLongList(DeviceRepositoryKey key, List<long> dataSet = null)
        {
            if (!cache.ContainsKey(key))
                cache.Add(key, key.ToString());

            var strArr = string.Empty;

            if (dataSet != null)
            {
                for (int i = 0; i < dataSet.Count; i++)
                {
                    strArr += dataSet[i];

                    if (i < dataSet.Count - 1)
                    {
                        strArr = strArr + ",";
                    }
                }
            }

            PlayerPrefs.SetString(key.ToString(), strArr);
        }

        public static void SaveKeyForIntList(DeviceRepositoryKey key, List<int> dataSet = null)
        {
            if (!cache.ContainsKey(key))
                cache.Add(key, key.ToString());

            var strArr = string.Empty;

            if (dataSet != null)
            {
                for (int i = 0; i < dataSet.Count; i++)
                {
                    strArr += dataSet[i];

                    if (i < dataSet.Count - 1)
                    {
                        strArr = strArr + ",";
                    }
                }
            }

            PlayerPrefs.SetString(key.ToString(), strArr);
        }

        public static bool LoadKeyForBoolean(DeviceRepositoryKey key, bool defaultValue)
        {
            if (!cache.ContainsKey(key))
                cache.Add(key, key.ToString());
            if (!PlayerPrefs.HasKey(cache[key]))
                SaveKeyForBoolean(key, defaultValue);
            return PlayerPrefs.GetInt(cache[key]) == 1 ? true : false;
        }

        public static bool LoadAndSaveKeyForBoolean(DeviceRepositoryKey key, bool defaultValue, bool newValue)
        {
            var ret = LoadKeyForBoolean(key, defaultValue);
            SaveKeyForBoolean(key, newValue);
            return ret;
        }

        public static int LoadKeyForInt(DeviceRepositoryKey key, int defaultValue)
        {
            if (!cache.ContainsKey(key))
                cache.Add(key, key.ToString());
            if (!PlayerPrefs.HasKey(cache[key]))
                SaveKeyForInt(key, defaultValue);
            return PlayerPrefs.GetInt(cache[key]);
        }

        public static float LoadKeyForFloat(DeviceRepositoryKey key, float defaultValue)
        {
            if (!cache.ContainsKey(key))
                cache.Add(key, key.ToString());
            if (!PlayerPrefs.HasKey(cache[key]))
                SaveKeyForFloat(key, defaultValue);
            return PlayerPrefs.GetInt(cache[key]);
        }

        public static string LoadKeyForString(DeviceRepositoryKey key, string defaultValue)
        {
            if (!cache.ContainsKey(key))
                cache.Add(key, key.ToString());
            if (!PlayerPrefs.HasKey(cache[key]))
                SaveKeyForString(key, defaultValue);
            return PlayerPrefs.GetString(cache[key]);
        }

        public static List<long> LoadKeyForLongList(DeviceRepositoryKey key)
        {
            if (!cache.ContainsKey(key))
                cache.Add(key, key.ToString());
            if (!PlayerPrefs.HasKey(key.ToString()))
                SaveKeyForLongList(key);

            List<long> LongTypeList = new();
            var stringValue = PlayerPrefs.GetString(key.ToString());

            if (!stringValue.Equals(string.Empty))
            {
                string[] stringSet = stringValue.Split(',');
                for (int i = 0; i < stringSet.Length; i++)
                {
                    LongTypeList.Add(System.Convert.ToInt64(stringSet[i]));
                }
            }

            return LongTypeList;
        }

        public static List<int> LoadKeyForIntList(DeviceRepositoryKey key)
        {
            if (!cache.ContainsKey(key))
                cache.Add(key, key.ToString());
            if (!PlayerPrefs.HasKey(key.ToString()))
                SaveKeyForLongList(key);

            List<int> intTypeList = new();
            var stringValue = PlayerPrefs.GetString(key.ToString());

            if (!stringValue.Equals(string.Empty))
            {
                string[] stringSet = stringValue.Split(',');
                for (int i = 0; i < stringSet.Length; i++)
                {
                    if (int.TryParse(stringSet[i], out var res))
                    {
                        intTypeList.Add(res);
                    }
                }
            }

            return intTypeList;
        }

        public static void LoadPlayerPrefs()
        {
        }

        #region Long List

        private static Dictionary<DeviceRepositoryKey, List<long>> _longListCache = new();

        private static List<long> LoadLongList(DeviceRepositoryKey key)
        {
            if (_longListCache.ContainsKey(key))
                return _longListCache[key];

            var list = LoadKeyForLongList(key);
            _longListCache.Add(key, list);
            return list;
        }

        public static void ClearLongList(DeviceRepositoryKey key)
        {
            if (_longListCache.ContainsKey(key))
                _longListCache[key].Clear();
            
            PlayerPrefs.DeleteKey(key.ToString());
        }
        
        public static void AddValueToLongList(DeviceRepositoryKey key, long value, bool saveOption = true)
        {
            var list = LoadLongList(key);
            list.Add(value);
            if (saveOption)
                SaveKeyForLongList(key, list);
        }

        public static void SaveLongList(DeviceRepositoryKey key)
        {
            var list = LoadLongList(key);
            SaveKeyForLongList(key, list);
        }

        public static bool ContainsForIntList(DeviceRepositoryKey key, long value)
        {
            var list = LoadLongList(key);
            return list.Contains(value);
        }

        #endregion

        #region Int List

        private static Dictionary<DeviceRepositoryKey, List<int>> _intListCache = new();

        private static List<int> LoadIntList(DeviceRepositoryKey key)
        {
            if (_intListCache.ContainsKey(key))
                return _intListCache[key];

            var list = LoadKeyForIntList(key);
            _intListCache.Add(key, list);
            return list;
        }

        public static void ClearIntList(DeviceRepositoryKey key)
        {
            if (_intListCache.ContainsKey(key))
                _intListCache[key].Clear();

            PlayerPrefs.DeleteKey(key.ToString());
        }

        public static void AddValueToIntList(DeviceRepositoryKey key, int value, bool saveOption = true)
        {
            var list = LoadIntList(key);
            list.Add(value);
            if (saveOption)
                SaveKeyForIntList(key, list);
        }

        public static void RemoveValueToIntList(DeviceRepositoryKey key, int value)
        {
            var list = LoadIntList(key);
            list.Remove(value);
            SaveKeyForIntList(key, list);
        }

        public static void SaveIntList(DeviceRepositoryKey key)
        {
            var list = LoadIntList(key);
            SaveKeyForIntList(key, list);
        }

        public static bool ContainsForIntList(DeviceRepositoryKey key, int value)
        {
            var list = LoadIntList(key);
            return list.Contains(value);
        }

        public static int CountForIntList(DeviceRepositoryKey key)
        {
            var list = LoadIntList(key);
            return list.Count;
        }

        #endregion
    }
}