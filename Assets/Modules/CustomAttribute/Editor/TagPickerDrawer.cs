using Modules.CustomAttribute;
using UnityEditorInternal;
using UnityEditor;
using UnityEngine;
using System;

namespace Modules.CustomAttributeEditor
{
	[CustomPropertyDrawer(typeof(TagPickerAttribute))]
	public class TagPickerDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			string[] tags = InternalEditorUtility.tags;
			float buttonSize = position.height + 10f;

			Rect fieldRect = new(position) { xMax = position.xMax - buttonSize - EditorGUIUtility.standardVerticalSpacing };
			Rect buttonRect = new(position) { x = fieldRect.xMax + EditorGUIUtility.standardVerticalSpacing, width = buttonSize };

			if (string.IsNullOrEmpty(property.stringValue))
				property.stringValue = tags[0];

			property.stringValue = DrawTagPicker(fieldRect, label.text, property.stringValue, tags);
		}

		private string DrawTagPicker(Rect position, string label, string text, string[] tags)
		{
			GUI.enabled = false;
			text = EditorGUI.TextField(new Rect(position) { xMax = position.xMax - position.height - EditorGUIUtility.standardVerticalSpacing }, label, text);

			GUI.enabled = tags.Length > 0;
			int index = Array.IndexOf(tags, text);
			index = EditorGUI.Popup(new Rect(position) { x = position.xMax - position.height, width = position.height }, index, tags);
			GUI.enabled = true;

			return index >= 0 ? tags[index] : text;
		}
	}
}