using Modules.CustomAttribute;
using UnityEditor;
using UnityEngine;

namespace Modules.CustomAttributeEditor
{
	[CustomPropertyDrawer(typeof(ChangeColorAttribute))]
	public class ChangeColorDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			ChangeColorAttribute param = (ChangeColorAttribute)attribute;

			GUI.backgroundColor = param.Color;
			GUI.color = param.Color;
			EditorGUI.PropertyField(position, property, label);
		}
	}
}