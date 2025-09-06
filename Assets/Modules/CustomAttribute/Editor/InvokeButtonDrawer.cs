using Modules.CustomAttribute;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System;

namespace Modules.CustomAttributeEditor 
{
	[CustomPropertyDrawer(typeof(InvokeButtonAttribute))]
	public class InvokeButtonDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			InvokeButtonAttribute buttonAttribute = (InvokeButtonAttribute)attribute;
			object target = GetTargetObjectOfProperty(property);
			if (target == null) return;

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
				MethodInfo method = targetType.GetMethod(buttonAttribute.MethodName,
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

				if (method != null)
				{
					method.Invoke(target, null);
				}
				else
				{
					Debug.LogWarning($"Method '{buttonAttribute.MethodName}' not found on {targetType.Name}");
				}
			}
		}

		private static object GetTargetObjectOfProperty(SerializedProperty property)
		{
			if (property == null) return null;

			object obj = property.serializedObject.targetObject;
			string path = property.propertyPath.Replace(".Array.data[", "[");
			string[] elements = path.Split('.');

			foreach (string element in elements)
			{
				if (element.Contains("["))
				{
					string elementName = element.Substring(0, element.IndexOf("["));
					int index = Convert.ToInt32(
						element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", "")
					);
					obj = GetValue(obj, elementName, index);
				}
				else
				{
					obj = GetValue(obj, element);
				}
			}
			return obj;
		}

		private static object GetValue(object source, string name)
		{
			if (source == null) return null;
			var type = source.GetType();
			var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			if (f != null) return f.GetValue(source);
			var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
			if (p != null) return p.GetValue(source, null);
			return null;
		}

		private static object GetValue(object source, string name, int index)
		{
			var enumerable = GetValue(source, name) as System.Collections.IEnumerable;
			if (enumerable == null) return null;
			var enm = enumerable.GetEnumerator();
			for (int i = 0; i <= index; i++)
				if (!enm.MoveNext()) return null;
			return enm.Current;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			System.Object target = property.serializedObject.targetObject;
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