using UnityEditor.UI;
using UnityEditor;

namespace ProjectHD.Editor
{
    [CustomEditor(typeof(SmartButton), true)]
    [CanEditMultipleObjects]
    /// <summary>
    ///   Custom Editor for the Button Component.
    ///   Extend this class to write a custom editor for a component derived from Button.
    /// </summary>
    public class SmartButtonEditor : SelectableEditor
    {
        SerializedProperty m_OnClickProperty;
        SerializedProperty _uiClickSoundProperty;
        SerializedProperty _delayTimeProperty;
        SerializedProperty _setInteractableProperty;
        SerializedProperty _useShareDelayProperty;

        SerializedProperty _canvasGroupProperty;
        SerializedProperty _activeAlphaValueProperty;
        SerializedProperty _inactiveAlphaValueProperty;
        //SerializedProperty _mixerGroupTypeProperty;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_OnClickProperty = serializedObject.FindProperty("m_OnClick");
            _uiClickSoundProperty = serializedObject.FindProperty("_uiClickSound");
            _delayTimeProperty = serializedObject.FindProperty("_delayTime");
            _useShareDelayProperty = serializedObject.FindProperty("_useShareDelay");
            _setInteractableProperty = serializedObject.FindProperty("_setInteractable");

            _canvasGroupProperty = serializedObject.FindProperty("_canvasGroup");
            _activeAlphaValueProperty = serializedObject.FindProperty("_activeAlphaValue");
            _inactiveAlphaValueProperty = serializedObject.FindProperty("_inactiveAlphaValue");
            //_mixerGroupTypeProperty = serializedObject.FindProperty("_mixerGroupType");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_OnClickProperty);
            EditorGUILayout.PropertyField(_uiClickSoundProperty);
            EditorGUILayout.PropertyField(_useShareDelayProperty);
            EditorGUILayout.PropertyField(_delayTimeProperty);
            EditorGUILayout.PropertyField(_setInteractableProperty);
            //EditorGUILayout.PropertyField(_mixerGroupTypeProperty);

            EditorGUILayout.PropertyField(_canvasGroupProperty);
            if (_canvasGroupProperty.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(_activeAlphaValueProperty);
                EditorGUILayout.PropertyField(_inactiveAlphaValueProperty);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}