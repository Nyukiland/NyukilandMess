using Modules.CustomAttribute;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Modules.CustomAttributeEditor
{
	[CustomPropertyDrawer(typeof(HideWhenBoolAttribute))]
	public class HideWhenBoolDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			HideWhenBoolAttribute parameters = (HideWhenBoolAttribute)attribute;
			string controllingPropertyPath = $"{GetParentPath(property)}{parameters.PropertyString}";
			SerializedProperty controllingProperty = property.serializedObject.FindProperty(controllingPropertyPath);

			bool isVisible = controllingProperty is { propertyType: SerializedPropertyType.Boolean }
							  && controllingProperty.boolValue == parameters.PropertyBool;

			if (isVisible) EditorGUI.PropertyField(position, property, label, true);

		}

		private string GetParentPath(SerializedProperty property)
		{
			string[] pathParts = property.propertyPath.Split('.');
			return pathParts.Length > 1 ? string.Join(".", pathParts.Take(pathParts.Length - 1)) + "." : string.Empty;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			HideWhenBoolAttribute parameters = (HideWhenBoolAttribute)attribute;
			string controllingPropertyPath = $"{GetParentPath(property)}{parameters.PropertyString}";
			SerializedProperty controllingProperty = property.serializedObject.FindProperty(controllingPropertyPath);

			bool isVisible = controllingProperty is { propertyType: SerializedPropertyType.Boolean }
							  && controllingProperty.boolValue == parameters.PropertyBool;

			if (isVisible)
				return base.GetPropertyHeight(property, label);
			else
				return 0f;
		}
	}
}