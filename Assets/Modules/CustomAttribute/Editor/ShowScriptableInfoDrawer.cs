using Modules.CustomAttribute;
using UnityEditor;
using UnityEngine;

namespace Modules.CustomAttributeEditor
{
	[CustomPropertyDrawer(typeof(ShowScriptableInfoAttribute))]
	public class ShowScriptableInfoDrawer : PropertyDrawer
	{
		private const float PADDING = 4f;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			DrawSingle(position, property, label);
		}

		private void DrawSingle(Rect position, SerializedProperty property, GUIContent label)
		{
			property.isExpanded = EditorGUI.Foldout(
				new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
				property.isExpanded,
				label,
				true
			);

			if (!property.isExpanded)
				return;

			float y = position.y + EditorGUIUtility.singleLineHeight + PADDING;

			Rect fieldRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(fieldRect, property, GUIContent.none);
			y += EditorGUIUtility.singleLineHeight + PADDING;

			if (property.objectReferenceValue is ScriptableObject so)
			{
				var serializedSO = new SerializedObject(so);
				serializedSO.Update();

				SerializedProperty prop = serializedSO.GetIterator();
				prop.NextVisible(true);

				EditorGUI.indentLevel++;
				while (prop.NextVisible(false))
				{
					float h = EditorGUI.GetPropertyHeight(prop, true);
					Rect r = new Rect(position.x, y, position.width, h);
					EditorGUI.PropertyField(r, prop, true);
					y += h + PADDING;
				}
				EditorGUI.indentLevel--;

				serializedSO.ApplyModifiedProperties();
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float total = EditorGUIUtility.singleLineHeight + PADDING;

			if (!property.isExpanded) return total;

			total += EditorGUIUtility.singleLineHeight + PADDING;

			if (property.objectReferenceValue is ScriptableObject so)
			{
				var serializedSO = new SerializedObject(so);
				SerializedProperty prop = serializedSO.GetIterator();
				prop.NextVisible(true);

				while (prop.NextVisible(false))
					total += EditorGUI.GetPropertyHeight(prop, true) + PADDING;
			}

			return total;

		}
	}
}