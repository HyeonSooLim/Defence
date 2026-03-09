using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class AnimEventEditor : EditorWindow
{
    public enum FunctionName
    {
        SpawnEffect,
        SendDamage,
    }

    private GameObject character;
    private AnimationClip animationClip;
    private float animationTime = 0f;

    private Animator characterAnimator;

    private FunctionName eventFunctionName = FunctionName.SpawnEffect;
    private string addressableName = null;
    //private Object spawnTransform = null;

    [MenuItem("Tools/ProjectHD/AnimEvent Editor")]
    public static void ShowWindow()
    {
        GetWindow<AnimEventEditor>("AnimEvent Editor");
    }

    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnGUI()
    {
        GUILayout.Label("AnimEvent Editor", EditorStyles.boldLabel);

        GameObject newCharacter = (GameObject)EditorGUILayout.ObjectField("Character", character, typeof(GameObject), true);
        if (newCharacter != character)
        {
            character = newCharacter;
            if (character != null)
            {
                characterAnimator = character.GetComponent<Animator>();
            }
        }

        animationClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", animationClip, typeof(AnimationClip), false);

        if (character == null || animationClip == null)
        {
            EditorGUILayout.HelpBox("Assign a character and an animation clip to preview.", MessageType.Info);
            return;
        }

        float newTime = EditorGUILayout.Slider("Time", animationTime, 0f, animationClip.length);
        if (!Mathf.Approximately(newTime, animationTime))
        {
            animationTime = newTime;
            SampleAnimation();
        }

        EditorGUILayout.Space();
        GUILayout.Label("Add Animation Event", EditorStyles.boldLabel);

        // Enum 값을 배열로 변환
        string[] enumNames = System.Enum.GetNames(typeof(FunctionName));
        int selectedIndex = (int)eventFunctionName;

        // 버튼처럼 보이는 툴바 생성
        selectedIndex = GUILayout.Toolbar(selectedIndex, enumNames);

        // 선택된 인덱스를 Enum으로 다시 변환
        eventFunctionName = (FunctionName)selectedIndex;

        // 선택된 값 출력 (디버그용)
        EditorGUILayout.LabelField("Selected Function:", eventFunctionName.ToString());

        //eventFunctionName = EditorGUILayout.EnumPopup("Function Name", eventFunctionName);
        addressableName = EditorGUILayout.TextField("Addressable Name (optional)", addressableName);
        //spawnTransform = EditorGUILayout.ObjectField("Transform Parameter (optional)", spawnTransform, typeof(Object), true);

        if (GUILayout.Button("Add Event at Current Time"))
        {
            AddAnimationEvent(animationClip, animationTime, eventFunctionName.ToString(), addressableName);
        }

        if (GUILayout.Button("Clear All Events at Current Clip"))
        {
            ClearAnimationEvent();
        }

        if (GUILayout.Button("Save (변경 사항 즉시 반영)"))
        {
            SaveAsset();
        }
    }

    private void OnEditorUpdate()
    {
        if (character == null || animationClip == null) return;

        SampleAnimation();
    }

    private void SampleAnimation()
    {
        if (character == null || animationClip == null) return;
        if (!AnimationMode.InAnimationMode())
            AnimationMode.StartAnimationMode();

        AnimationMode.BeginSampling();
        AnimationMode.SampleAnimationClip(character, animationClip, animationTime);
        AnimationMode.EndSampling();

        SceneView.RepaintAll();
        EditorApplication.QueuePlayerLoopUpdate();
    }

    //private void AddAnimationEvent(AnimationClip clip, float time, string functionName, Object targetObj, string addressableName)
    private void AddAnimationEvent(AnimationClip clip, float time, string functionName, string addressableName)
    {
        if (clip == null || string.IsNullOrEmpty(functionName)) return;

        var events = AnimationUtility.GetAnimationEvents(clip);

        var newEvent = new AnimationEvent
        {
            time = time,
            functionName = functionName,
            //objectReferenceParameter = targetObj,
            stringParameter = addressableName
        };

        var updatedEvents = new AnimationEvent[events.Length + 1];
        for (int i = 0; i < events.Length; i++)
            updatedEvents[i] = events[i];

        updatedEvents[events.Length] = newEvent;
        AnimationUtility.SetAnimationEvents(clip, updatedEvents);

        //Debug.Log($"Added AnimationEvent '{functionName}' at time {time:F2}s with object: {targetObj?.name ?? "None"} Addressable Name: {addressableName ?? "None"}");
        Debug.Log($"Added AnimationEvent '{functionName}' at time {time:F2}s with object: Addressable Name: {addressableName ?? "None"}");
    }

    private void ClearAnimationEvent()
    {
        if (animationClip == null) return;
        AnimationUtility.SetAnimationEvents(animationClip, new AnimationEvent[0]);
        Debug.Log("Cleared all AnimationEvents from the clip.");
    }

    private void SaveAsset()
    {
        AssetDatabase.SaveAssets();
    }
}
