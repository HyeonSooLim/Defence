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

#if UNITY_IOS
        //public const string FileURL = "https://data.dsc4.net/dsc4_CDN/ko/application_config1.txt";
#else
        //public const string FileURL = "http://data.dsc4.net/dsc4_CDN/ko/application_config1.txt";
#endif

        public const string UNITY_VERSION = "2022.3.62f";
        public const string OneStorePackageName = "Temp_OneStore";
        public const string PlayStorePackageName = "Temp_PlayStore";
        public const string NaverCafeURL = "https://cafe.naver.com/";
        public const int MaxPlayerLife = 3;

#endregion

        public static AudioMixer AudioMixer { get; private set; }
        public static AudioMixerGroup BackgroundMixerGroup { get; private set; }
        public static AudioMixerGroup EnvironmentalMixerGroup { get; private set; }
        public static AudioMixerGroup EffectMixerGroup { get; private set; }
        public static AudioMixerGroup UIMixerGroup { get; private set; }
        public static AudioMixerGroup VoiceMixerGroup { get; private set; }
        //public static Dictionary<MixerGroupType, AudioMixerGroup> MixerGroupsIndex { get; private set; }
        public static CameraSettings CameraSettings { get; private set; }
        public static bool DisableResourceCaching { get; set; }
        public static bool DisableSceneCaching { get; set; }

        public static int ScreenWidth { get; private set; }
        public static int ScreenHeight { get; private set; }

        public static float FadeInDuration { get; private set; }
        public static float FadeOutDuration { get; private set; }
        public static float SoundFadeDuration { get; private set; }

        static StaticValue()
        {
        }

        public static async UniTask LoadAsync()
        {
            await PreLoadAsync();
        }

        private static async UniTask PreLoadAsync()
        {
            //AudioMixer = await Resources.LoadAsync<AudioMixer>("GameAudioMixer") as AudioMixer;
            CameraSettings = await Resources.LoadAsync<CameraSettings>("CameraSettings") as CameraSettings;

            //BackgroundMixerGroup = AudioMixer.FindMatchingGroups("Background")[0];
            //EnvironmentalMixerGroup = AudioMixer.FindMatchingGroups("Environmental")[0];
            //EffectMixerGroup = AudioMixer.FindMatchingGroups("Effect")[0];
            //UIMixerGroup = AudioMixer.FindMatchingGroups("UI")[0];
            //VoiceMixerGroup = AudioMixer.FindMatchingGroups("Voice")[0];
            //MixerGroupsIndex = new();
            //MixerGroupsIndex.Add(MixerGroupType.None, null);
            //MixerGroupsIndex.Add(MixerGroupType.Background, BackgroundMixerGroup);
            //MixerGroupsIndex.Add(MixerGroupType.Environmental, EnvironmentalMixerGroup);
            //MixerGroupsIndex.Add(MixerGroupType.Effect, EffectMixerGroup);
            //MixerGroupsIndex.Add(MixerGroupType.UI, UIMixerGroup);
            //MixerGroupsIndex.Add(MixerGroupType.Voice, VoiceMixerGroup);
            
            ScreenWidth = Screen.width;
            ScreenHeight = Screen.height;

            UniTask.DelayFrame(5);
        }
    }
}