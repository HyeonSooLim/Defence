using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace ProjectHD
{
    public static class StaticValue
    {
        #region ConstPart
        public const long MemoryLimit = 2000 * 1024 * 1024;

        public const string UNITY_VERSION = "2022.3.62f";
        public const string OneStorePackageName = "com.ProjectHD.Temp_OneStore";
        public const string PlayStorePackageName = "com.ProjectHD.Temp_PlayStore";
        public const string NaverCafeURL = "https://cafe.naver.com/";
        public const int MaxPlayerLife = 3;

#endregion

        public static CameraSettings CameraSettings { get; private set; }

        public static int ScreenWidth { get; private set; }
        public static int ScreenHeight { get; private set; }

        static StaticValue()
        {
        }

        public static async UniTask LoadAsync()
        {
            await PreLoadAsync();
        }

        private static async UniTask PreLoadAsync()
        {
            CameraSettings = await Resources.LoadAsync<CameraSettings>("CameraSettings") as CameraSettings;
            
            ScreenWidth = Screen.width;
            ScreenHeight = Screen.height;

            await UniTask.DelayFrame(5);
        }
    }
}