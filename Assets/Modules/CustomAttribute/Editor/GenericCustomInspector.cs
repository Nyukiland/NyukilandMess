using Modules.CustomAttribute;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Modules.CustomAttributeEditor
{
	[CustomEditor(typeof(MonoBehaviour), true)]
	[CanEditMultipleObjects]
	public class GenericCustomInspector : Editor
	{
		private Stack<bool> foldoutStack = new();

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			foldoutStack.Clear();

			SerializedProperty iterator = serializedObject.GetIterator();
			bool enterChildren = true;

			while (iterator.NextVisible(enterChildren))
			{
				enterChildren = false;

				if (iterator.propertyPath == "m_Script")
				{
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.PropertyField(iterator);
					EditorGUI.EndDisabledGroup();
					continue;
				}

				FieldInfo fieldInfo = ReflectionCache.GetFieldInfo(target.GetType(), iterator.name);

				BeginFoldoutAttribute beginAttr = fieldInfo?.GetCustomAttribute<BeginFoldoutAttribute>();
				EndFoldoutAttribute endAttr = fieldInfo?.GetCustomAttribute<EndFoldoutAttribute>();

				if (endAttr != null && foldoutStack.Count > 0)
					foldoutStack.Pop();

				if (beginAttr != null)
				{
					bool open = GetFoldoutOpen(target.GetType(), beginAttr.Title);

					Rect rect = GUILayoutUtility.GetRect(0, beginAttr.TitleSize, GUILayout.ExpandWidth(true));
					open = EditorGUI.Foldout(rect, open, beginAttr.Title, true);

					SetFoldoutOpen(target.GetType(), beginAttr.Title, open);

					foldoutStack.Push(open);
				}

				bool shouldDraw = foldoutStack.Count == 0 || foldoutStack.Peek();

				if (shouldDraw)
				{
					EditorGUILayout.PropertyField(iterator, true);
				}
			}

			InvokeButtonDisplayer.DrawInspectorButtons(serializedObject.targetObject);
			serializedObject.ApplyModifiedProperties();
		}

		#region Foldout
		private static string GetKey(Type inspectedType, string foldoutTitle)
		{
			return $"Foldout.{inspectedType.FullName}.{foldoutTitle}";
		}

		private static bool GetFoldoutOpen(Type inspectedType, string foldoutTitle)
		{
			return EditorPrefs.GetBool(GetKey(inspectedType, foldoutTitle), true);
		}

		private static void SetFoldoutOpen(Type inspectedType, string foldoutTitle, bool isOpen)
		{
			EditorPrefs.SetBool(GetKey(inspectedType, foldoutTitle), isOpen);
		}
		#endregion
	}
}