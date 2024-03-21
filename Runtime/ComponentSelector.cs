#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FPS
{
    public class ComponentSelectorAttribute : PropertyAttribute
    {
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ComponentSelectorAttribute))]
    public class ComponentSelectorPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.type.Contains("Component"))
            {
                EditorGUI.BeginProperty(position, label, property);
                if (property.serializedObject.targetObject is not Component owner)
                    return;

                var componentsList = owner.GetComponents<Component>();
                string[] types = new string[componentsList.Length];

                for (int i = 0; i < types.Length; i++)
                {
                    types[i] = componentsList[i].GetType().Name;
                }

                int index = 0;
                for (int i = 0; i < componentsList.Length; i++)
                {
                    if (componentsList[i] != property.objectReferenceValue)
                        continue;

                    index = i;
                    break;
                }

                index = EditorGUI.Popup(position, label.text, index, types);
                property.objectReferenceValue = componentsList[index];

                EditorGUI.EndProperty();
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }
#endif
}