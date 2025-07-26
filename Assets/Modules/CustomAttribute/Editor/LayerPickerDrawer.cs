using Modules.CustomAttribute;
using UnityEditorInternal;
using UnityEditor;
using UnityEngine;
using System;

namespace Modules.CustomAttributeEditor
{
	[CustomPropertyDrawer(typeof(LayerPickerAttribute))]
	public class LayerPickerDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			string[] layers = InternalEditorUtility.layers;
			float buttonSize = position.height + 10f;

			Rect fieldRect = new(position) { xMax = position.xMax - buttonSize - EditorGUIUtility.standardVerticalSpacing };
			Rect buttonRect = new(position) { x = fieldRect.xMax + EditorGUIUtility.standardVerticalSpacing, width = buttonSize };

			if (string.IsNullOrEmpty(property.stringValue))
				property.stringValue = layers[0];

			property.stringValue = DrawLayerPicker(fieldRect, label.text, property.stringValue, layers);
		}

		private string DrawLayerPicker(Rect position, string label, string text, string[] layers)
		{
			GUI.enabled = false;
			text = EditorGUI.TextField(new Rect(position) { xMax = position.xMax - position.height - EditorGUIUtility.standardVerticalSpacing }, label, text);

			GUI.enabled = layers.Length > 0;
			int index = Array.IndexOf(layers, text);
			index = EditorGUI.Popup(new Rect(position) { x = position.xMax - position.height, width = position.height }, index, layers);
			GUI.enabled = true;

			return index >= 0 ? layers[index] : text;
		}
	}
}