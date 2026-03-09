using DG.DemiEditor;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;

using ProjectHD;

namespace UnityToolbarExtender.Examples
{
    static class ToolbarStyles
    {
        public static readonly GUIStyle commandButtonStyle;

        static ToolbarStyles()
        {
            commandButtonStyle = new GUIStyle("Command")
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                imagePosition = ImagePosition.ImageAbove,
                fontStyle = FontStyle.Bold,
            };
        }
    }

    [InitializeOnLoad]
    public class SceneSwitchLeftButton
    {
        static SceneSwitchLeftButton()
        {
            ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
        }

        static void OnToolbarGUI()
        {
            GUILayout.FlexibleSpace();
            var temp = ToolbarStyles.commandButtonStyle;
            temp.fixedWidth = 60;

            var currentPlatform = Application.dataPath;
            if (currentPlatform.Contains("PlayStore"))
                currentPlatform = "PlayStore";
            else if (currentPlatform.Contains("OneStore"))
                currentPlatform = "OneStore";
            else
                currentPlatform = string.Empty;

#if UNITY_IOS
                currentPlatform = "iOS";
#endif

            if (!string.IsNullOrEmpty(currentPlatform))
            {
                var platformGui = temp.Clone();
                platformGui.fixedWidth = 90;
                if (GUILayout.Button(currentPlatform.ToString(), platformGui))
                {
                    Debug.Log(currentPlatform);
                }
            }

            var tempStreamline = temp.Clone();
            tempStreamline.fixedWidth = 48;
            if (GUILayout.Button(new GUIContent("시작", "씬"), tempStreamline))
            {
                SceneHelper.StartScene(ProjectEnum.SceneName.MainWorkSpace.ToString());
            }

            var newGui = temp.Clone();
            newGui.fixedWidth = 100;
            if (GUILayout.Button(new GUIContent(SceneHelper.lastScene, "최근 열었던 씬"), newGui))
            {
                string[] guids = AssetDatabase.FindAssets("t:scene " + SceneHelper.lastScene, null);
                if (guids.Length == 0)
                {
                    Debug.LogWarning("Couldn't find scene file");
                }
                else
                {
                    string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);

                    EditorApplication.ExitPlaymode();
                    EditorSceneManager.OpenScene(scenePath);
                }
            }
        }
    }

    static class SceneHelper
    {
        static string sceneToOpen;

        public static string lastScene
        {
            get => EditorPrefs.GetString("LastConnectScene", "");
            set => EditorPrefs.SetString("LastConnectScene", value);
        }

        public static void StartScene(string sceneName)
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            var last = EditorSceneManager.GetActiveScene().name;
            if (last != ProjectEnum.SceneName.MainWorkSpace.ToString())
            {
                lastScene = EditorSceneManager.GetActiveScene().name;
            }

            sceneToOpen = sceneName;
            EditorApplication.update += OnUpdate;
        }

        static void OnUpdate()
        {
            if (sceneToOpen == null ||
                EditorApplication.isPlaying || EditorApplication.isPaused ||
                EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorApplication.update -= OnUpdate;

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                // need to get scene via search because the path to the scene
                // file contains the package version so it'll change over time
                string[] guids = AssetDatabase.FindAssets("t:scene " + sceneToOpen, null);
                if (guids.Length == 0)
                {
                    Debug.LogWarning("Couldn't find scene file");
                }
                else
                {
                    string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    EditorSceneManager.OpenScene(scenePath);
                    EditorApplication.isPlaying = true;
                }
            }
            sceneToOpen = null;
        }
    }
}