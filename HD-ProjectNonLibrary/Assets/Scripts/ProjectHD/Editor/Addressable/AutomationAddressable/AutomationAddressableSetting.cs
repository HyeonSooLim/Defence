using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;

namespace ProjectHD.Editor
{
    public enum AutoSearchOption
    {
        [InspectorName("사용하지않음")] None,
        [InspectorName("해당 경로의 파일만 포함")] OnlyFiles,
        [InspectorName("해당 폴더의 파일만 포함")] Folder,
        [InspectorName("해당 경로의 하위 디렉토리 포함")] SearchSubdirectories,
    }

    [System.Serializable]
    public class AddressableCustomPathData
    {
        public string DevName;
        public  List<string> PathList;
        public string GroupName;
        public AutoSearchOption SearchOption = AutoSearchOption.None;
    }
    
    [System.Serializable]
    public class AddressablePathData
    {
        public string DevName;
        public string Path;
        public string GroupName;
        public string FileExtensionName;
        public AutoSearchOption SearchOption = AutoSearchOption.None;
    }
    
    [System.Serializable]
    public class AddressableDigimonData
    {
        public int Seed;
        public string DigimonName;
        public string DigimonNameKey;
        [SerializeReference] public Dictionary<string, List<AddressableDataSubAssetData>> SubAssetDataList = new Dictionary<string, List<AddressableDataSubAssetData>>();
    }
    
    [System.Serializable]
    public class AddressableDataSubAssetData
    {
        public Object _object;
        public string _fullPath;
        public string _unityPath;
        public string _GUID;
    }

    [CreateAssetMenu(fileName = "EditorDataBase", menuName = "ScriptableObjects/Editor/EditorDataBase")]
    public class AutomationAddressableSetting : ScriptableObject
    {
        [TextArea] public string Description;
        public List<string> AutoAddressGroupList;
        public AddressableAssetSettings AddressableAssetSetting;
        public List<AddressableCustomPathData> CustomPathData = new List<AddressableCustomPathData>();
        [SerializeReference] public Dictionary<string, List<AddressableDataSubAssetData>> CustomDataDict = new Dictionary<string, List<AddressableDataSubAssetData>>();

        public void Clear()
        {
            CustomDataDict.Clear();
        }
    }
}