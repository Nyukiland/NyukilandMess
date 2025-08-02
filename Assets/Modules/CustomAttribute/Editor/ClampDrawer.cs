using Modules.CustomAttribute;
using UnityEditor;
using UnityEngine;

namespace Modules.CustomAttributeEditor 
{
	[CustomPropertyDrawer(typeof(ClampAttribute))]
	public class ClampDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			ClampAttribute clamp = (ClampAttribute)attribute;

			EditorGUI.BeginChangeCheck();
			EditorGUI.PropertyField(position, property, label, true);

			if (EditorGUI.EndChangeCheck())
			{
				if (property.propertyType == SerializedPropertyType.Float)
					property.floatValue = Mathf.Clamp(property.floatValue, clamp.Min, clamp.Max);

				else if (property.propertyType == SerializedPropertyType.Integer)
					property.intValue = Mathf.Clamp(property.intValue, (int)clamp.Min, (int)clamp.Max);
			}
		}
	}
}