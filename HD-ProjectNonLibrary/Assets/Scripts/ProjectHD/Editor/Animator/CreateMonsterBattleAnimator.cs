using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DigimonSC.Editor
{
    public class CreateMonsterBattleAnimator : OdinEditorWindow
    {
        [MenuItem("Tools/ProjectHD/CreateMonsterBattleAnimator")]
        public static void ShowEditor()
        {
            var wnd = GetWindow<CreateMonsterBattleAnimator>();
        }

        [BoxGroup("Input"), AssetsOnly, SerializeField]
        public RuntimeAnimatorController AnimatorController;
        [BoxGroup("Input"), AssetsOnly, SerializeField]
        public Object[] SourceAnimationFolders;

        [BoxGroup("Output"), AssetsOnly, SerializeField]
        public Object AnimatorControllerFolder;

        [Button(ButtonSizes.Large)]
        public void CreateAnimatorController()
        {
            foreach (var sourceFolder in SourceAnimationFolders)
            {
                CreateAnimatorController(sourceFolder, AnimatorControllerFolder);
            }

            AssetDatabase.Refresh();
        }

        public AnimatorOverrideController CreateAnimatorController(Object animationFolder, Object outputFolder)
        {
            // 폴더 경로 보정
            string folderPath = AssetDatabase.GetAssetPath(animationFolder);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                folderPath = System.IO.Path.GetDirectoryName(folderPath);
            }

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.LogError($"유효한 폴더가 아닙니다: {animationFolder.name}");
                return null;
            }

            // 출력 경로
            string outputFolderPath = AssetDatabase.GetAssetPath(outputFolder);
            string animatorControllerPath = $"{outputFolderPath}/{animationFolder.name}.overrideController";

            // 오버라이드 컨트롤러 생성/로드
            var animatorController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(animatorControllerPath);
            if (!animatorController)
            {
                animatorController = new AnimatorOverrideController(AnimatorController);
                AssetDatabase.CreateAsset(animatorController, animatorControllerPath);
            }

            // 원본 Animator Controller의 모든 클립 가져오기
            List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new();
            animatorController.GetOverrides(overrides);

            // 폴더 안의 모든 AnimationClip 로드 (소문자 딕셔너리)
            var assetGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folderPath });
            var animationClips = assetGuids
                .Select(guid => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(clip => clip != null)
                .ToDictionary(clip => clip.name.ToLower(), clip => clip);

            // 자동 매핑
            for (int i = 0; i < overrides.Count; i++)
            {
                var originalClip = overrides[i].Key;
                string lowerName = originalClip.name.ToLower();

                if (animationClips.TryGetValue(lowerName, out var newClip))
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(originalClip, newClip);
                }
                else
                {
                    // 부분 매칭 시도
                    var found = animationClips.FirstOrDefault(kvp => lowerName.Contains(kvp.Key));
                    if (found.Value != null)
                    {
                        overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(originalClip, found.Value);
                    }
                }
            }

            // Fallback 로직(Attack02가 없다면 Attack01로 대체 등)
            void Fallback(string target, string source)
            {
                var t = overrides.FirstOrDefault(kvp => kvp.Key.name == target);
                var s = overrides.FirstOrDefault(kvp => kvp.Key.name == source);
                if (t.Value == null && s.Value != null)
                {
                    int idx = overrides.FindIndex(kvp => kvp.Key.name == target);
                    overrides[idx] = new KeyValuePair<AnimationClip, AnimationClip>(t.Key, s.Value);
                }
            }

            // 예시: Attack01 → Attack02, Attack03
            Fallback("Attack02", "Attack01");
            Fallback("Attack03", "Attack01");
            Fallback("Attack05", "Attack04");
            Fallback("Attack06", "Attack04");
            Fallback("Defense02", "Defense01");
            Fallback("Defense03", "Defense01");
            Fallback("Defense05", "Defense04");
            Fallback("Defense06", "Defense04");
            Fallback("Heal02", "Heal01");
            Fallback("Heal03", "Heal01");
            Fallback("Heal05", "Heal04");
            Fallback("Heal06", "Heal04");
            Fallback("Defensive02", "Defensive01");
            Fallback("Defensive03", "Defensive01");
            Fallback("Hit02", "Hit01");
            Fallback("Hit03", "Hit01");
            Fallback("Die02", "Die01");
            Fallback("Die03", "Die01");
            Fallback("Dead02", "Dead01");
            Fallback("Dead03", "Dead01");

            // 적용 및 저장
            animatorController.ApplyOverrides(overrides);
            EditorUtility.SetDirty(animatorController);
            AssetDatabase.SaveAssets();

            return animatorController;
        }
    }
}
