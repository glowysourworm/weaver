using UnityEditor;

using UnityEngine;

namespace Assets
{
    public class DiagnosticAttribute : PropertyAttribute
    {

    }

    [CustomPropertyDrawer(typeof(DiagnosticAttribute))]
    public class DiagnosticDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property,
                                                GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position,
                                   SerializedProperty property,
                                   GUIContent label)
        {
            GUI.enabled = false;

            string valueStr = "";

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    valueStr = property.intValue.ToString();
                    break;
                case SerializedPropertyType.Boolean:
                    valueStr = property.boolValue.ToString();
                    break;
                case SerializedPropertyType.Float:
                    valueStr = property.floatValue.ToString("0.00000");
                    break;
                case SerializedPropertyType.String:
                    valueStr = property.stringValue;
                    break;
                case SerializedPropertyType.Enum:
                {
                    // Caught one error for this one; but not every time... (hmm.)
                    if (property.enumValueIndex >= 0 &&
                        property.enumValueIndex < property.enumDisplayNames.Length)
                        valueStr = property.enumDisplayNames[property.enumValueIndex];
                }
                break;
                default:
                    valueStr = "(not supported)";
                    break;
            }

            EditorGUI.LabelField(position, label.text, valueStr);

            GUI.enabled = true;
        }
    }
}
