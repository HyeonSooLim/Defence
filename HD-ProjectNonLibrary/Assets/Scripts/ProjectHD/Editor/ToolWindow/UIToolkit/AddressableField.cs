using UnityEditor;
using UnityEngine.UIElements;

class AddressableField : VisualElement
{
    SerializedProperty m_serializedProperty = null;
    public SerializedProperty serializedProperty
    {
        get { return m_serializedProperty; }
        set { m_serializedProperty = value; }
    }
    IMGUIContainer m_container = null;
 
    public AddressableField()
    {
        m_container = new IMGUIContainer();
        m_container.onGUIHandler += OnGUI;
        m_container.style.flexGrow = 1.0f;
        Add(m_container);
    }
 
    void OnGUI()
    {
        if (m_serializedProperty != null)
        {
            EditorGUILayout.PropertyField(m_serializedProperty);
        }
    }
 
    public new class UxmlFactory : UxmlFactory<AddressableField> { }
}