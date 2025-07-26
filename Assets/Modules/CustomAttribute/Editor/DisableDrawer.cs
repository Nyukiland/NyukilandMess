using UnityEditor;
using UnityEngine;

namespace Modules.CustomAttribute
{
	[CustomPropertyDrawer(typeof(DisableAttribute))]
	public class DisableDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			GUI.enabled = false;
			EditorGUI.PropertyField(position, property, label, true);
		}
	}
}