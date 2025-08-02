using Modules.CustomAttribute;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Modules.CustomAttributeEditor 
{
	[CustomPropertyDrawer(typeof(InvokeButtonAttribute))]
	public class InvokeButtonDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			InvokeButtonAttribute buttonAttribute = (InvokeButtonAttribute)attribute;
			Object target = property.serializedObject.targetObject;
			System.Type targetType = target.GetType();
			FieldInfo fieldInfo = targetType.GetField(property.name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

			string warning = null;

			if (fieldInfo != null)
			{
				bool hasSerializeField = fieldInfo.GetCustomAttribute<SerializeField>() != null;

				if (!fieldInfo.IsPrivate || !hasSerializeField)
					warning = "Field should be private and marked with [SerializeField] for best practice.";

				if (fieldInfo.FieldType != typeof(byte))
					warning = (warning != null ? warning + "\n" : "") + "Field should be of type 'byte' to minimize memory usage.";

				if (!string.IsNullOrEmpty(warning))
				{
					Rect warningRect = new (position.x, position.y, position.width, 40);
					EditorGUI.HelpBox(warningRect, warning, MessageType.Warning);
					position.y += 42;
				}
			}

			Rect buttonRect = new (position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

			if (GUI.Button(buttonRect, buttonAttribute.MethodName))
			{
				MethodInfo method = targetType.GetMethod(buttonAttribute.MethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

				if (method != null)
				{
					method.Invoke(target, null);
				}
				else
				{
					UnityEngine.Debug.LogWarning($"Method '{buttonAttribute.MethodName}' not found on {targetType.Name}");
				}
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			Object target = property.serializedObject.targetObject;
			System.Type targetType = target.GetType();
			FieldInfo fieldInfo = targetType.GetField(property.name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

			float height = EditorGUIUtility.singleLineHeight + 6;

			if (fieldInfo != null)
			{
				bool isPrivate = fieldInfo.IsPrivate;
				bool hasSerializeField = fieldInfo.GetCustomAttribute<SerializeField>() != null;
				bool isByte = fieldInfo.FieldType == typeof(byte);

				if (!isPrivate || !hasSerializeField || !isByte)
					height += 42;
			}

			return height;
		}
	}
}