using Cysharp.Threading.Tasks;
using System;
using System.IO;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Networking;
using Utilities;

#if UNITY_IOS || UNITY_IPHONE 
using UnityEditor.iOS.Xcode;
#endif
using Debug = UnityEngine.Debug;

namespace ProjectHD.Editor
{
    public class Builder
    {
        public const string EDITOR_Build = "Tools/Build";
        public const string EDITOR_BuildAddressable = "Tools/BuildAddressable";

        public static string build_script = "Assets/AddressableAssetsData/DataBuilders/BuildScriptPackedMode.asset";
        public static string settings_asset = "Assets/AddressableAssetsData/AddressableAssetSettings.asset";
        public static string profile_name = "CDN";
        private static AddressableAssetSettings settings;

        private static readonly string StoreKitFramework = "StoreKit.framework";
        private static readonly string SecurityFramework = "Security.framework";
        private static readonly string SystemConfigurationFramework = "SystemConfiguration.framework";
        private static readonly string AdSupportFramework = "AdSupport.framework";
        private static readonly string WebKitFramework = "WebKit.framework";
        private static readonly string LibSqlit3Tbd = "libsqlite3.0.tbd";
        private static readonly string LibzTbd = "libz.tbd";
        private static readonly string AdServicesFramework = "AdServices.framework";

        private static readonly string EntitlementsPath = "Entitlements.entitlements";

#if UNITY_IOS
    [PostProcessBuild]
    public static void OnPostBuild(BuildTarget target, string pathToBuildProject)
    {

            // Add Game Center capability. Required since Unity and Apple F***ed everything up with Xcode 15.
            string projectPath = PBXProject.GetPBXProjectPath(pathToBuildProject);
            PBXProject project = new PBXProject();
            project.ReadFromString(File.ReadAllText(projectPath));

            string unityTargetGuid = project.GetUnityFrameworkTargetGuid();
            string mainTargetGuid = project.GetUnityMainTargetGuid();
            
            {
                //Gamebase
                TryAddFrameToProject(project, unityTargetGuid, StoreKitFramework, false);
            }
            {
                //Singular
                TryAddFrameToProject(project, unityTargetGuid, SecurityFramework, false);
                TryAddFrameToProject(project, unityTargetGuid, SystemConfigurationFramework, false);
                TryAddFrameToProject(project, unityTargetGuid, AdSupportFramework, false);
                TryAddFrameToProject(project, unityTargetGuid, WebKitFramework, false);
                TryAddFrameToProject(project, unityTargetGuid, LibSqlit3Tbd, false);
                TryAddFrameToProject(project, unityTargetGuid, LibzTbd, false);
                TryAddFrameToProject(project, unityTargetGuid, StoreKitFramework, false);
                TryAddFrameToProject(project, unityTargetGuid, AdServicesFramework, true);
            }
        
            
            File.WriteAllText(projectPath, project.WriteToString());

            var manager = new ProjectCapabilityManager(projectPath, EntitlementsPath, null, mainTargetGuid);
            manager.AddSignInWithApple();
#if DEVELOPMENT_BUILD
            manager.AddPushNotifications(true);
#else
            manager.AddPushNotifications(false);
#endif
            manager.AddInAppPurchase();
            manager.AddBackgroundModes(BackgroundModesOptions.RemoteNotifications);
            manager.WriteToFile();
            
            //////////////////////////
        
            
            string plistPath = pathToBuildProject + "/Info.plist";
            PlistDocument plistObj = new PlistDocument();
        
            // Read the values from the plist file:
            plistObj.ReadFromString(File.ReadAllText(plistPath));
 
            // Set values from the root object:
            PlistElementDict plistRoot = plistObj.root;
 
            // Set the description key-value in the plist:
            plistRoot.SetString("NSUserTrackingUsageDescription", "허용된 데이터는 안전하게 보호되며 게임사와 광고사의 향상된 맞춤형 광고 타겟팅에만 활용됩니다.");
 
            // Save changes to the plist:
            File.WriteAllText(plistPath, plistObj.WriteToString());
            
            void TryAddFrameToProject(PBXProject pbxProject, string targetGuid, string framework, bool weak)
            {
                if (pbxProject.ContainsFramework(targetGuid, framework)) return;
                
                pbxProject.AddFrameworkToProject(targetGuid, framework, weak);
            }
    }
    

#endif

        static bool GetSettingsObject(string settingsAsset)
        {
            // This step is optional, you can also use the default settings:
            //settings = AddressableAssetSettingsDefaultObject.Settings;

            settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(settingsAsset)
                as AddressableAssetSettings;

            if (settings == null)
            {
                Debug.LogError($"[LogCode][ErrorCode] {settingsAsset} couldn't be found or isn't " +
                               $"a settings object.");
                return false;
            }

            return true;
        }

        static bool SetProfile(string profile)
        {
            string profileId = settings.profileSettings.GetProfileId(profile);
            if (String.IsNullOrEmpty(profileId))
            {
                Debug.LogWarning($"Couldn't find a profile named, {profile}, " + $"using current profile instead.");
                return false;
            }
            else
            {
                settings.activeProfileId = profileId;
                return true;
            }
        }

        static void setBuilder(IDataBuilder builder)
        {
            int index = settings.DataBuilders.IndexOf((ScriptableObject)builder);

            if (index > 0)
                settings.ActivePlayerDataBuilderIndex = index;
            else
                Debug.LogWarning($"{builder} must be added to the " +
                                 $"DataBuilders list before it can be made " +
                                 $"active. Using last run builder instead.");
        }

        static bool buildAddressableContent(bool clearCache = false)
        {
            if (clearCache)
            {
                AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder.ClearCachedData();
            }

            AddressableAssetSettings
                .BuildPlayerContent(out AddressablesPlayerBuildResult result);
            bool success = string.IsNullOrEmpty(result.Error);

            if (!success)
            {
                Debug.LogError($"[LogCode][ErrorCode]Addressables build error encountered: {result.Error}");
            }

            return success;
        }

        public static int BuildAddressables(string version, bool clearCache = false)
        {
            PlayerSettings.bundleVersion = version;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            if (!GetSettingsObject(settings_asset))
                return 0;
            if (!SetProfile(profile_name))
                return 0;

            IDataBuilder builderScript = AssetDatabase.LoadAssetAtPath<ScriptableObject>(build_script) as IDataBuilder;

            if (builderScript == null)
            {
                Debug.LogError($"[ErrorCode] {build_script} couldn't be found or isn't a build script.");
                return 0;
            }

            setBuilder(builderScript);

            return buildAddressableContent(clearCache) ? 1 : 0;
        }

        public static int ResourceBuild()
        {
            string version = "1.0.1";

            string[] args = System.Environment.GetCommandLineArgs();

            int index = 0;

            foreach (string arg in args)
            {
                InternalDebug.LogBuild($"[LogCode] ({index}): {arg}");
                if (arg.StartsWith("-PLATFORMTYPE"))
                {
                    InternalDebug.LogBuild($"[LogCode] -PLATFORMTYPE : {args[index + 1]}");
                }
                else if (arg.StartsWith("-VERSION_CODE"))
                {
                    version = args[index + 1];
                    InternalDebug.LogBuild($"[LogCode] -VERSION_CODE : {version}");
                }

                index++;
            }

            return BuildAddressables(version);
        }

        public static int Build()
        {
            string buildPath = "D:\\Company\\Export\\";
            string fileName = "DSC";
            string version = "1.0.1";
            int versionCode = 1;
            bool devMode = true;
            bool cleanBuild = true;
            bool appBundle = true;
            ProjectEnum.PlatformMarketType marketType = ProjectEnum.PlatformMarketType.PlayStore;

            string[] args = System.Environment.GetCommandLineArgs();

            int index = 0;
            var tempBuildPath = buildPath;


            foreach (string arg in args)
            {
                InternalDebug.LogBuild($"[LogCode] ({index}): {arg}");
                if (arg.StartsWith("-PLATFORMTYPE"))
                {
                    InternalDebug.LogBuild($"[LogCode] -PLATFORMTYPE : {args[index + 1]}");
                }
                else if (arg.StartsWith("-BUILD_PATH"))
                {
                    buildPath = args[index + 1];
                    InternalDebug.LogBuild($"[LogCode] -BUILD_PATH : {buildPath}");
                }
                else if (arg.StartsWith("-FILENAME"))
                {
                    fileName = args[index + 1];
                    InternalDebug.LogBuild($"[LogCode] -FILENAME : {fileName}");
                }
                else if (arg.StartsWith("-BUNDLE_VERSION"))
                {
                    versionCode = Convert.ToInt32(args[index + 1]);
                    InternalDebug.LogBuild($"[LogCode] -BUNDLE_VERSION : {versionCode}");
                }
                else if (arg.StartsWith("-VERSION_CODE"))
                {
                    version = args[index + 1];
                    InternalDebug.LogBuild($"[LogCode] -VERSION_CODE : {version}");
                }
                else if (arg.StartsWith("-DEV_BUILD"))
                {
                    devMode = Convert.ToBoolean(args[index + 1]);
                    InternalDebug.LogBuild($"[LogCode] -DEV_BUILD : {devMode}");
                }
                else if (arg.StartsWith("-CLEAN_BUILD"))
                {
                    cleanBuild = Convert.ToBoolean(args[index + 1]);
                    InternalDebug.LogBuild($"[LogCode] -CLEAN_BUILD : {cleanBuild}");
                }
                else if (arg.StartsWith("-APPBUNDLE"))
                {
                    appBundle = Convert.ToBoolean(args[index + 1]);
                    InternalDebug.LogBuild($"[LogCode] -APPBUNDLE : {appBundle}");
                }
                else if (arg.StartsWith("-MARKETTYPE"))
                {
                    // marketType = Convert.ToBoolean(args[index + 1]);
                    InternalDebug.LogBuild($"[LogCode] -MARKETTYPE : {marketType}");
                }
                index++;
            }
#if UNITY_ANDROID || UNITY_EDITOR
            return BuildAOS(version, versionCode, buildPath, fileName, devMode, cleanBuild, appBundle, marketType);
#elif UNITY_IOS
        return BuildIOS(version, buildPath, fileName, devMode, cleanBuild);
#endif
        }

        public static int BuildAOS(string buildVersion,
            int bundleVersionCode,
            string locationPath,
            string fileName,
            bool devMode,
            bool cleanBuild,
            bool isAppBundle,
            ProjectEnum.PlatformMarketType marketType)
        {
            DateTime now = DateTime.Now;
            string option = "";
            option += devMode ? "[DevMode]" : "";
            option += cleanBuild ? "[CleanBuild]" : "";
            var path = "";

            switch (marketType)
            {
                case ProjectEnum.PlatformMarketType.PlayStore:
                    PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, StaticValue.PlayStorePackageName);
                    option += "[PlayStore]";
                    break;
                case ProjectEnum.PlatformMarketType.OneStore:
                    PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, StaticValue.OneStorePackageName);
                    option += "[OneStore]";
                    break;
                case ProjectEnum.PlatformMarketType.AppStore:
                    break;
            }

#if QA_BUILD
        option += "[QA]";
#endif

            if (isAppBundle)
            {
                path = locationPath + fileName + $"_{buildVersion}_{now.ToString("yyMMdd_HH_mm_ss")}_{option}.aab";
            }
            else
            {
                path = locationPath + fileName + $"_{buildVersion}_{now.ToString("yyMMdd_HH_mm_ss")}_{option}.apk";
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            PlayerSettings.bundleVersion = buildVersion;
            PlayerSettings.Android.bundleVersionCode = bundleVersionCode;
            PlayerSettings.Android.keystorePass = "dkfvkxla";
            PlayerSettings.Android.keyaliasPass = "dkfvkxla";

            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                EditorUserBuildSettings.buildAppBundle = isAppBundle;
            }

            // BuildSettings에서 Android 설정 로드
            buildPlayerOptions.scenes = GetScenePaths();
            buildPlayerOptions.locationPathName = path; // 빌드된 APK의 저장 경로

            buildPlayerOptions.target = BuildTarget.Android;
            buildPlayerOptions.options = BuildOptions.None;

            if (devMode)
            {
                buildPlayerOptions.options |= BuildOptions.Development;
                buildPlayerOptions.options |= BuildOptions.AllowDebugging;
            }

            if (cleanBuild)
            {
                buildPlayerOptions.options |= BuildOptions.CleanBuildCache;
            }

            InternalDebug.LogBuild(buildPlayerOptions.options.ToString());
            // return 0;
            // 빌드 시작
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                InternalDebug.LogBuild($"[LogCode] Build succeeded" +
                          $"\nData: {summary.totalSize} bytes" +
                          $"\nTime: {summary.totalTime} seconds");
                return 0;
            }
            else
            {
                Debug.LogError("[LogCode][ErrorCode] Build failed");
                return 1;
            }
        }

        public static int BuildAOS(string buildVersion,
            int bundleVersionCode,
            string locationPath,
            string appIdentifier,
            string keyStorePass,
            string keyAliasPass,
            bool devMode,
            bool cleanBuild,
            bool isAppBundle,
            string market)
        {
            DateTime now = DateTime.Now;
            string option = "";
            option += devMode ? "[Development]" : "";
            option = $"{option}[{now:yyMMdd_HHmm}][{market}]";

#if QA_BUILD
        option += "[QA]";
#endif

            string fileName = string.Empty;

            if (isAppBundle)
            {
                fileName = "DSC4" + $"_[{bundleVersionCode}][{buildVersion}]{option}.aab";
            }
            else
            {
                fileName = "DSC4" + $"_[{bundleVersionCode}][{buildVersion}]{option}.apk";
            }

            var filePath = Path.Combine(locationPath, fileName);

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            PlayerSettings.applicationIdentifier = appIdentifier;
            PlayerSettings.bundleVersion = buildVersion;
            PlayerSettings.Android.bundleVersionCode = bundleVersionCode;
            PlayerSettings.Android.keystorePass = keyStorePass;
            PlayerSettings.Android.keyaliasPass = keyAliasPass;

            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                EditorUserBuildSettings.buildAppBundle = isAppBundle;
            }

            // BuildSettings에서 Android 설정 로드
            buildPlayerOptions.scenes = GetScenePaths();
            buildPlayerOptions.locationPathName = filePath; // 빌드된 APK의 저장 경로

            buildPlayerOptions.target = BuildTarget.Android;
            buildPlayerOptions.options = BuildOptions.None;

            if (devMode)
            {
                buildPlayerOptions.options |= BuildOptions.Development;
                buildPlayerOptions.options |= BuildOptions.AllowDebugging;
            }

            if (cleanBuild)
            {
                buildPlayerOptions.options |= BuildOptions.CleanBuildCache;
            }

            InternalDebug.LogBuild(buildPlayerOptions.options.ToString());
            // return 0;
            // 빌드 시작
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                InternalDebug.LogBuild($"[LogCode] Build succeeded" +
                          $"\nData: {summary.totalSize} bytes" +
                          $"\nTime: {summary.totalTime} seconds");
                return 0;
            }
            else
            {
                Debug.LogError("[LogCode][ErrorCode] Build failed");
                throw new System.Exception("[LogCode][ErrorCode] Build failed");
                return 1;
            }
        }

        public static int BuildIOS(string buildVersion,
           string locationPath,
           string fileName,
           bool devMode,
           bool cleanBuild)
        {
            DateTime now = DateTime.Now;
            // string option = "";
            // option += devMode ? "[DevMode]" : "";
            // option += cleanBuild ? "[CleanBuild]" : "";
            // var path = locationPath + fileName + $"_{buildVersion}_{option}" + now.ToString("yyMMdd_HH_mm_ss") + ".apk";

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            PlayerSettings.bundleVersion = buildVersion;

            // BuildSettings에서 IOS 설정 로드
            buildPlayerOptions.scenes = GetScenePaths();
            buildPlayerOptions.locationPathName = locationPath; // 빌드된 APK의 저장 경로
            buildPlayerOptions.target = BuildTarget.iOS;
            buildPlayerOptions.options = BuildOptions.None;

            if (devMode)
            {
                buildPlayerOptions.options |= BuildOptions.Development;
                buildPlayerOptions.options |= BuildOptions.AllowDebugging;
            }

            if (cleanBuild)
            {
                buildPlayerOptions.options |= BuildOptions.CleanBuildCache;
            }

            InternalDebug.LogBuild(buildPlayerOptions.options.ToString());
            // return 0;
            // 빌드 시작
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                InternalDebug.LogBuild($"[LogCode] Build succeeded" +
                          $"\nData: {summary.totalSize} bytes" +
                          $"\nTime: {summary.totalTime} seconds");
                return 0;
            }
            else
            {
                Debug.LogError("[LogCode][ErrorCode] Build failed");
                return 1;
            }
        }

        public static int BuildIOS(string buildVersion,
            string locationPath,
            bool devMode,
            bool cleanBuild,
            bool append,
            string market)
        {
            DateTime now = DateTime.Now;
            // string option = "";
            // option += devMode ? "[DevMode]" : "";
            // option += cleanBuild ? "[CleanBuild]" : "";
            // var path = locationPath + fileName + $"_{buildVersion}_{option}" + now.ToString("yyMMdd_HH_mm_ss") + ".apk";

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            PlayerSettings.bundleVersion = buildVersion;

            // BuildSettings에서 IOS 설정 로드
            buildPlayerOptions.scenes = GetScenePaths();
            buildPlayerOptions.locationPathName = locationPath;
            buildPlayerOptions.target = BuildTarget.iOS;
            buildPlayerOptions.options = BuildOptions.None;

            if (devMode)
            {
                buildPlayerOptions.options |= BuildOptions.Development;
                buildPlayerOptions.options |= BuildOptions.AllowDebugging;
            }

            if (cleanBuild)
            {
                buildPlayerOptions.options |= BuildOptions.CleanBuildCache;
            }

            if (append)
            {
                buildPlayerOptions.options |= BuildOptions.AcceptExternalModificationsToPlayer;
            }

            InternalDebug.LogBuild(buildPlayerOptions.options.ToString());
            // return 0;
            // 빌드 시작
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                InternalDebug.LogBuild($"[LogCode] Build succeeded" +
                                       $"\nData: {summary.totalSize} bytes" +
                                       $"\nTime: {summary.totalTime} seconds");
                return 0;
            }
            else
            {
                Debug.LogError("[LogCode][ErrorCode] Build failed");
                throw new System.Exception("[LogCode][ErrorCode] Build failed");
                return 1;
            }
        }

        // 현재 BuildSettings에 추가된 모든 씬의 경로를 가져오는 메소드
        static string[] GetScenePaths()
        {
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            string[] scenePaths = new string[sceneCount];
            for (int i = 0; i < sceneCount; i++)
            {
                scenePaths[i] = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            }

            return scenePaths;
        }

        private static string GeneratePassword(string apiKey, string date)
        {
            var encoding = new System.Text.UTF8Encoding();
            byte[] keyBytes = encoding.GetBytes(apiKey);
            byte[] messageBytes = encoding.GetBytes(date);

            using (var hmacsha1 = new System.Security.Cryptography.HMACSHA1(keyBytes))
            {
                byte[] hashmessage = hmacsha1.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashmessage);
            }
        }

        public static void UploadFTPDir(string ftpUrl, string username, string password, string localDirPath)
        {
            if (!Directory.Exists(localDirPath))
            {
                Debug.LogError("Directory not found: " + localDirPath);
                return;
            }

            string[] files = Directory.GetFiles(localDirPath, "*.*", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                string relativePath = file.Substring(localDirPath.Length + 1).Replace("\\", "/");
                string uri = ftpUrl + "/" + relativePath;
                UploadFTP(uri, username, password, file);
            }
        }

        public static void UploadFTP(string ftpUrl, string username, string password, string localFilePath)
        {
            if (!File.Exists(localFilePath))
            {
                InternalDebug.LogErrorBuild("File not found: " + localFilePath);
                return;
            }

            FileInfo fileInfo = new FileInfo(localFilePath);
            string uri = ftpUrl;

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential(username, password);
            request.UseBinary = true;
            request.ContentLength = fileInfo.Length;

            byte[] fileContents;
            using (StreamReader sourceStream = new StreamReader(localFilePath))
            {
                fileContents = System.Text.Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
            }

            request.ContentLength = fileContents.Length;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(fileContents, 0, fileContents.Length);
            }

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                InternalDebug.LogBuild("Upload File Complete, status " + response.StatusDescription);
            }
        }

        public async static void SendPostRequest()
        {
            string username = "dsc-cdn@moveint.io";
            string apiKey = "56613NPIHRixNd3ZX46myNoLXFgkam";
            string date = DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", new System.Globalization.CultureInfo("en-US"));
            string password = GeneratePassword(apiKey, date);

            string url = "https://api.cdnetworks.com/ccm/purge/ItemIdReceiver";

            // JSON 데이터 생성
            string jsonData = @"
        {
            ""urls"": [
                ""https://dsc-cdn.gameking.com/dsc4/ko/application_config.txt""
            ],
            ""urlAction"": ""default""
        }";

            // UnityWebRequest 설정
            UnityWebRequest request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Date", date);
            request.SetRequestHeader("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password)));

            var operation = request.SendWebRequest();
            // 요청 보내기
            while (!operation.isDone)
            {
                await UniTask.Yield();
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                InternalDebug.LogBuild("Request sent successfully: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }
        }

        public async static void SendPostRequest1()
        {
            string username = "dsc-cdn@moveint.io";
            string apiKey = "56613NPIHRixNd3ZX46myNoLXFgkam";
            string date = DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", new System.Globalization.CultureInfo("en-US"));
            string password = GeneratePassword(apiKey, date);

            string url = "https://api.cdnetworks.com/ccm/purge/ItemIdReceiver";

            // JSON 데이터 생성
            string jsonData = @"
        {
            ""urls"": [
                ""https://dsc-cdn.gameking.com/dsc4/ko/application_config1.txt""
            ],
            ""urlAction"": ""default""
        }";

            // UnityWebRequest 설정
            UnityWebRequest request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Date", date);
            request.SetRequestHeader("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password)));

            var operation = request.SendWebRequest();
            // 요청 보내기
            while (!operation.isDone)
            {
                await UniTask.Yield();
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                InternalDebug.LogBuild("Request sent successfully: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }
        }

        public async static void SendDirectoryPostRequest(string link)
        {
            string username = "dsc-cdn@moveint.io";
            string apiKey = "56613NPIHRixNd3ZX46myNoLXFgkam";
            string date = DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", new System.Globalization.CultureInfo("en-US"));
            string password = GeneratePassword(apiKey, date);

            string url = "https://api.cdnetworks.com/ccm/purge/ItemIdReceiver";

            // JSON 데이터 생성
            string jsonData = @"
        {
            ""urls"": [
                ""https://dsc-cdn.gameking.com/" + link + @"""
            ],
            ""urlAction"": ""default""
        }";

            // UnityWebRequest 설정
            UnityWebRequest request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Date", date);
            request.SetRequestHeader("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password)));

            var operation = request.SendWebRequest();
            // 요청 보내기
            while (!operation.isDone)
            {
                await UniTask.Yield();
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                InternalDebug.LogBuild($"Request sent successfully: targetLink(https://dsc-cdn.gameking.com/{link})" + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }
        }
    }
}