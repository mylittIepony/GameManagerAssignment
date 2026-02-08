using System;
using UnityEditor;
using UnityEngine;

namespace SymmetryBreakStudio.TastyGrassShader.Editor
{
    [CustomPropertyDrawer(typeof(DisableGroupAttribute))]
    public class DisableGroupAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DisableGroupAttribute disableGroupAttribute = (DisableGroupAttribute)attribute;

            SerializedProperty disableProperty =
                property.serializedObject.FindProperty(
                    AttributeCommon.ResolveRelativePath(property.propertyPath, disableGroupAttribute.valueName));

            bool prevState = GUI.enabled;
            if (disableProperty == null)
            {
                Debug.LogError($"Unable to find property \"{disableGroupAttribute.valueName}\".",
                    property.serializedObject.targetObject);
            }
            else
            {
                int value;
                switch (disableGroupAttribute.valueType)
                {
                    case ValueType.Int:
                        value = disableProperty.intValue;
                        break;
                    case ValueType.BoolAsInt:
                        value = disableProperty.boolValue ? 1 : 0;
                        break;
                    case ValueType.FloatAsInt:
                        value = Mathf.CeilToInt(disableProperty.floatValue);
                        break;
                    case ValueType.EnumAsInt:
                        value = disableProperty.enumValueIndex;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                bool conditionMeet = value == disableGroupAttribute.compareValue;
                conditionMeet ^= disableGroupAttribute.invertCondition;
                GUI.enabled = !conditionMeet;
            }

            EditorGUI.PropertyField(position, property, label);
            GUI.enabled = prevState;
        }
    }
}