using NUnit.Framework.Constraints;
//using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectHD.Editor
{
    public enum TestVersionType
    {
        Alpha,
        Beta,
        Live,
    }

    [System.Serializable]
    public class SMTMIgnoreItemData
    {
        public int itemSeed;
    }

    [CreateAssetMenu(fileName = "IntegratedToolSetting", menuName = "ScriptableObjects/Editor/IntegratedToolSetting")]
    public class IntegratedToolSetting : ScriptableObject
    {
        [Header("빌드 관련 변수")]
        public string buildPath = "D:\\Company\\Unity\\Export\\";
        public string fileName = "DSC";
        public string alphaVersion = "1.0.0";
        public string betaVersion = "1.0.0.0";
        public string liveVersion = "3.2.4";
        public bool useDevBuild = true;
        public bool cleanBuild = true;
        public bool appBundle = false;

        [Header("안드로이드 빌드 관련 변수")]
        public ProjectEnum.PlatformMarketType marketType = ProjectEnum.PlatformMarketType.PlayStore;
        public int bundleNumber = 1;

        [Header("FTP 관련 변수")]
        public string ftpHost = "upload-web.nas.nefficient.com";
        public string ftpPath = "dsc4/ko/AOS/1.0.0/Patch01/Android";
        public string ftpUserName = "dsc-cdn";
        public string ftpPassword = "비밀입니당 ^^";
        public string ftpResourcePath = "";

        public string incorrectAddressablePath = "";
        public List<int> SMTMIgnoreItemData;

        [Header("ExcelToJson 관련 변수")]
        public string BinaryOutputFolder = "Assets/Project/GameResources/MessagePackBinary";
        public string JsonOutputFolder = "Assets/Project/GameResources/MessagePackJson";

        [Header("자동 코드 생성 관련 변수")]
        // public string GenerateScriptPath = "Assets/Project/Scripts";
        public string MainNameSpace = "Project";
    }
}