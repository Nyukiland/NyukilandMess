using Modules.CustomAttribute;
using UnityEditor;
using UnityEngine;

namespace Modules.CustomAttributeEditor
{
	[CustomPropertyDrawer(typeof(RequiredAttribute))]
	public class RequiredDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			bool isValid = true;
			string message = null;

			switch (property.propertyType)
			{
				case SerializedPropertyType.ObjectReference:
					isValid = property.objectReferenceValue != null;
					message = "Reference is required.";
					break;

				case SerializedPropertyType.String:
					isValid = !string.IsNullOrEmpty(property.stringValue);
					message = "String cannot be empty.";
					break;

				case SerializedPropertyType.Integer:
					isValid = property.intValue != 0;
					message = "Value cannot be zero.";
					break;

				case SerializedPropertyType.Float:
					isValid = property.floatValue != 0f;
					message = "Value cannot be zero.";
					break;
			}

			if (!isValid)
			{
				Rect helpRect = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight * 1.5f);
				EditorGUI.HelpBox(helpRect, message, MessageType.Error);
				position.y += EditorGUIUtility.singleLineHeight * 1.5f + 2;
			}

			EditorGUI.PropertyField(position, property, label, true);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float baseHeight = EditorGUI.GetPropertyHeight(property, label, true);

			bool needsHelpBox = false;

			switch (property.propertyType)
			{
				case SerializedPropertyType.ObjectReference:
					needsHelpBox = property.objectReferenceValue == null;
					break;

				case SerializedPropertyType.String:
					needsHelpBox = string.IsNullOrEmpty(property.stringValue);
					break;

				case SerializedPropertyType.Integer:
					needsHelpBox = property.intValue == 0;
					break;

				case SerializedPropertyType.Float:
					needsHelpBox = property.floatValue == 0f;
					break;
			}

			if (needsHelpBox)
				return baseHeight + EditorGUIUtility.singleLineHeight * 1.5f + 4;
			else
				return baseHeight;
		}
	}
}