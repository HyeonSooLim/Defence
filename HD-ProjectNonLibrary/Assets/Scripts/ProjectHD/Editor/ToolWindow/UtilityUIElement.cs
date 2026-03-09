//using Core.Editor.UIToolkit;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.GUI;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

//TODO 추후 네임스페이스 이름 변경
namespace ProjectHD.Editor
{
    public static class UtilityUIElement
    {
        public static Button CreateButton(string text, Action onClick = null)
        {
            Button button = new Button(onClick)
            {
                text = text
            };
            return button;
        }

        public static Foldout CreateFoldout(string title, bool collapsed = false, EventCallback<ChangeEvent<bool>> onValueChanged = null)
        {
            Foldout foldout = new Foldout()
            {
                text = title,
                value = !collapsed
            };
            if (onValueChanged != null)
            {
                foldout.RegisterValueChangedCallback(onValueChanged);
            }
            return foldout;
        }

        public static Port CreatePort(this Node node, string portName = "", Orientation orientation = Orientation.Horizontal, Direction direction = Direction.Output, Port.Capacity capacity = Port.Capacity.Single)
        {
            Port port = node.InstantiatePort(orientation, direction, capacity, typeof(bool));

            port.portName = portName;

            return port;
        }

        public static TextField CreateTextField(string value = null, string label = null, EventCallback<ChangeEvent<string>> onValueChanged = null)
        {
            TextField textField = new TextField()
            {
                value = value,
                label = label
            };
            
            if (onValueChanged != null)
            {
                textField.RegisterValueChangedCallback(onValueChanged);
            }

            return textField;
        }
        
        public static Vector3Field CreateVector3Field(Vector3 value, string label = null, EventCallback<ChangeEvent<Vector3>> onValueChanged = null)
        {
            Vector3Field textField = new Vector3Field()
            {
                value = value,
                label = label
            };

            if (onValueChanged != null)
            {
                textField.RegisterValueChangedCallback(onValueChanged);
            }

            return textField;
        }
        
        public static Vector2Field CreateVector2Field(Vector2 value, string label = null, EventCallback<ChangeEvent<Vector2>> onValueChanged = null)
        {
            Vector2Field textField = new Vector2Field()
            {
                value = value,
                label = label
            };

            if (onValueChanged != null)
            {
                textField.RegisterValueChangedCallback(onValueChanged);
            }

            return textField;
        }
        
        public static FloatField CreateFloatField(float value, string label = null, EventCallback<ChangeEvent<float>> onValueChanged = null)
        {
            FloatField textField = new FloatField()
            {
                value = value,
                label = label
            };

            if (onValueChanged != null)
            {
                textField.RegisterValueChangedCallback(onValueChanged);
            }

            return textField;
        }
        
        public static IntegerField CreateIntegerField(int value, string label = null, EventCallback<ChangeEvent<int>> onValueChanged = null)
        {
            IntegerField textField = new IntegerField()
            {
                value = value,
                label = label
            };

            if (onValueChanged != null)
            {
                textField.RegisterValueChangedCallback(onValueChanged);
            }

            return textField;
        }
        
        public static DoubleField CreateDoubleField(double value, string label = null, EventCallback<ChangeEvent<double>> onValueChanged = null)
        {
            DoubleField textField = new DoubleField()
            {
                value = value,
                label = label
            };

            if (onValueChanged != null)
            {
                textField.RegisterValueChangedCallback(onValueChanged);
            }

            return textField;
        }
        
        public static Slider CreateSlider(float value, string label = null, EventCallback<ChangeEvent<float>> onValueChanged = null)
        {
            Slider textField = new Slider()
            {
                value = value,
                label = label
            };

            if (onValueChanged != null)
            {
                textField.RegisterValueChangedCallback(onValueChanged);
            }

            return textField;
        }
        
        public static Toggle CreateToggle(bool value, string label = null, EventCallback<ChangeEvent<bool>> onValueChanged = null)
        {
            Toggle textField = new Toggle()
            {
                value = value,
                label = label
            };

            if (onValueChanged != null)
            {
                textField.RegisterValueChangedCallback(onValueChanged);
            }

            return textField;
        }

        public static VisualElement CreateHorizontal()
        {
            var horizontal = new VisualElement();
            horizontal.style.flexDirection = FlexDirection.Row;
            horizontal.style.width = Length.Percent(100);
            return horizontal;
        }

        public static SliderInt CreateSliderInt(int value, out VisualElement horizontal, string label = null, EventCallback<ChangeEvent<int>> onValueChanged = null)
        {
            horizontal = new VisualElement();
            horizontal.style.flexDirection = FlexDirection.Row;
            horizontal.style.width = Length.Percent(100);
            if (label != null)
            {
                var labelField = CreateLabel(label);
                labelField.style.width = Length.Percent(10);
                horizontal.Add(labelField);
            }

         

            SliderInt slider = new SliderInt()
            {
                value = value,
                label = null
            };
            
            IntegerField intField = new IntegerField()
            {
                value = value,
            };
            EventCallback<ChangeEvent<int>> a = evt =>
            {
                intField.SetValueWithoutNotify(evt.newValue);
                if (onValueChanged != null)
                    onValueChanged.Invoke(evt);
            };
            slider.RegisterValueChangedCallback(a);
            slider.style.width = Length.Percent(70);
            horizontal.Add(slider);
            

            intField.RegisterValueChangedCallback(_ =>
                {
                    slider.SetValueWithoutNotify(_.newValue);   
                    if (onValueChanged != null)
                        onValueChanged.Invoke(_);
                });
            intField.style.width = Length.Percent(20);
            horizontal.Add(intField);
 
            return slider;
        }
        
        public static EnumField CreateEnumField(Enum value, string label = null, EventCallback<ChangeEvent<Enum>> onValueChanged = null)
        {
            var enumField = new EnumField(label, value);
            
            if (onValueChanged != null)
                enumField.RegisterCallback(onValueChanged);

            return enumField;
        }
        
        public static EnumFlagsField CreateEnumFragsField(Enum value, string label = null, EventCallback<ChangeEvent<Enum>> onValueChanged = null)
        {
            var enumField = new EnumFlagsField(label, value);
            
            if (onValueChanged != null)
                enumField.RegisterCallback(onValueChanged);

            return enumField;
        }

        public static VisualElement CreateSpaceField(VisualElement parent, float size, bool isPercent = false)
        {
            var spaceField = new VisualElement();
            if (isPercent)
                spaceField.style.height = Length.Percent(size);
            else
                spaceField.style.height = size;
       
            parent.Add(spaceField);
            return spaceField;
        }

        public static DropdownField CreateDropdownField(string label, List<string> list, int defaultIndex = 0, EventCallback<ChangeEvent<string>> onValueChanged = null)
        {
            var dropdown = new DropdownField(label, list, defaultIndex);
            
            if (onValueChanged != null)
                dropdown.RegisterValueChangedCallback(onValueChanged);
            
            return dropdown;
        }
        
        public static ObjectField CreateObjectField<T>(T objectType, string label, EventCallback<ChangeEvent<UnityEngine.Object>> onValueChanged = null) where T : Object
        {
            var objectField = new ObjectField(label)
            {
                value = objectType,
                objectType = typeof(T),
                allowSceneObjects = false,
            };

            if (onValueChanged != null)
                objectField.RegisterValueChangedCallback(onValueChanged);
            
            return objectField;
        }
        
        public static ColorField CreateColorField(string label, Color color, EventCallback<ChangeEvent<Color>> onValueChanged = null)
        {
            var objectField = new ColorField(label)
            {
                value = color,
            };

            if (onValueChanged != null)
                objectField.RegisterValueChangedCallback(onValueChanged);
            
            return objectField;
        }

        public static PropertyField CreateAssetRefField(Object obj,  string label, string property, EventCallback<SerializedPropertyChangeEvent> onValueChanged = null)
        {
            var serializedObject = new SerializedObject(obj);
            
            //변수명 찾기
            var tempProperty = serializedObject.FindProperty(property);
            if (tempProperty == null)
            {
                Debug.LogError(property);
                return null;
            }
            
            //프로퍼티 필드 생성
            var propertyField = new PropertyField(tempProperty, label);
            propertyField.BindProperty(tempProperty);
            if (onValueChanged != null)
                propertyField.RegisterValueChangeCallback(onValueChanged);
            return propertyField;
        }
        
        public static PropertyField CreateAssetRefField(Object obj,  string label, string p1, string p2, EventCallback<SerializedPropertyChangeEvent> onValueChanged = null)
        {
            var serializedObject = new SerializedObject(obj);
            
            //변수명 1번 찾기
            var tempProperty = serializedObject.FindProperty(p1);
            if (tempProperty == null)
            {
                Debug.LogError($"{obj.name}에서 property1({p1})을 찾을수 없습니다.");
                return null;
            }
            
            //변수명 2번 찾기
            tempProperty = tempProperty.serializedObject.FindProperty(p2);
            if (tempProperty == null)
            {
                Debug.LogError($"{obj.name}에서 property2({p2})을 찾을수 없습니다.");
                return null;
            }
            
            //프로퍼티 필드 생성
            var propertyField = new PropertyField(tempProperty, label);
            propertyField.BindProperty(tempProperty);
            if (onValueChanged != null)
                propertyField.RegisterValueChangeCallback(onValueChanged);
            return propertyField;
        }
        
        public static PropertyField CreateAssetRefField(Object obj,  string label, string p1, string p2, string p3, EventCallback<SerializedPropertyChangeEvent> onValueChanged = null)
        {
            var serializedObject = new SerializedObject(obj);
            
            //변수명 1번 찾기
            var tempProperty = serializedObject.FindProperty(p1);
            if (tempProperty == null)
            {
                Debug.LogError($"{obj.name}에서 property1({p1})을 찾을수 없습니다.");
                return null;
            }
            
            //변수명 2번 찾기
            tempProperty = tempProperty.serializedObject.FindProperty(p2);
            if (tempProperty == null)
            {
                Debug.LogError($"{obj.name}에서 property2({p2})을 찾을수 없습니다.");
                return null;
            }
            
            //변수명 3번 찾기
            tempProperty = tempProperty.serializedObject.FindProperty(p3);
            if (tempProperty == null)
            {
                Debug.LogError($"{obj.name}에서 property3({p3})을 찾을수 없습니다.");
                return null;
            }
            
            //프로퍼티 필드 생성
            var propertyField = new PropertyField(tempProperty, label);
            propertyField.BindProperty(tempProperty);
            if (onValueChanged != null)
                propertyField.RegisterValueChangeCallback(onValueChanged);
            return propertyField;
        }
        
        public static List<PropertyField> CreateAssetRefMultiField(Object obj,  string label, List<string> propertyList, EventCallback<SerializedPropertyChangeEvent> onValueChanged = null)
        {
            if (propertyList.Count <= 0)
            {
                Debug.LogError("프로퍼티 리스트가 비어있습니다.");
                return null;
            }
            
            var tempObj = new SerializedObject(obj);
            var tempPropertyList = tempObj.FindProperty(propertyList[0]);
            List<PropertyField> tempProperties = new List<PropertyField>(); 
            if (tempPropertyList.isArray)
            {
                for (int i = 0; i < tempPropertyList.arraySize; i++)
                {
                    var tempProperty = tempPropertyList.GetArrayElementAtIndex(i);
                    if (tempProperty == null)
                        continue;
                    var propertyField = new PropertyField(tempProperty, label);
                    propertyField.BindProperty(tempProperty);
                    if (onValueChanged != null)
                        propertyField.RegisterValueChangeCallback(onValueChanged);
                    tempProperties.Add(propertyField);
                }
            }
            else
            {
                var propertyField = new PropertyField(tempPropertyList, label);
                propertyField.BindProperty(tempPropertyList);
                if (onValueChanged != null)
                    propertyField.RegisterValueChangeCallback(onValueChanged);
                tempProperties.Add(propertyField);
            }

            return tempProperties;
        }

        public static TextField CreateTextArea(string value = null, string label = null, EventCallback<ChangeEvent<string>> onValueChanged = null)
        {
            TextField textArea = CreateTextField(value, label, onValueChanged);

            textArea.multiline = true;

            return textArea;
        }
        
        public static Label CreateLabel(string value = EditorConst.EDITOR_DIALOG_NULL_STRING)
        {
            Label label = new Label()
            {
                text = value,
            };
            return label;
        }
    }
}

