using Modules.CustomAttribute;
using System;
using System.Reflection;
using UnityEngine;

namespace Modules.CustomAttributeEditor
{
	public static class InvokeButtonDisplayer
	{
		public static void DrawInspectorButtons(object target)
		{
			Type type = target.GetType();
			MethodInfo[] methods = type.GetMethods(BindingFlags.Instance |
				BindingFlags.Public | BindingFlags.NonPublic);

			foreach (MethodInfo method in methods)
			{
				InvokeButtonAttribute attr = method.GetCustomAttribute<InvokeButtonAttribute>();
				if (attr == null)
					continue;

				if (method.GetParameters().Length == 0 && method.ReturnType == typeof(void))
				{
					string label = attr.ButtonLabel ?? method.Name;

					if (GUILayout.Button(label))
					{
						method.Invoke(target, null);
					}
				}
				else
				{
					GUILayout.Label($"[{nameof(InvokeButtonDisplayer)}] " +
						$"{method.Name} must have no parameters and void return type.");
				}
			}

		}
	}
}