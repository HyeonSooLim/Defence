using UnityEngine;

namespace ProjectHD.Editor
{
    [CreateAssetMenu(fileName = "IntegratedToolSetting", menuName = "ScriptableObjects/Editor/IntegratedToolSetting")]
    public class IntegratedToolSetting : ScriptableObject
    {
        [Header("빌드 관련 변수")]
        public string buildPath = "D:\\Company\\Unity\\Export\\";
        public string fileName = "ProjectHD";
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
        public string ftpHost = "";
        public string ftpPath = "";
        public string ftpUserName = "";
        public string ftpPassword = "";
        public string ftpResourcePath = "";

        public string incorrectAddressablePath = "";

        [Header("ExcelToJson 관련 변수")]
        public string BinaryOutputFolder = "Assets/Project/GameResources/MessagePackBinary";
        public string JsonOutputFolder = "Assets/Project/GameResources/MessagePackJson";
    }
}