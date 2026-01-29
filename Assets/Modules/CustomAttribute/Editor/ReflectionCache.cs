using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Modules.CustomAttributeEditor
{
	[InitializeOnLoad]
	public static class ReflectionCache
	{
		private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> cache = new();

		static ReflectionCache()
		{
			AssemblyReloadEvents.beforeAssemblyReload += ClearCache;
		}

		public static void ClearCache()
		{
			cache.Clear();
		}

		public static FieldInfo GetFieldInfo(Type type, string fieldName)
		{
			if (!cache.TryGetValue(type, out var fields))
			{
				fields = new Dictionary<string, FieldInfo>();
				Type current = type;
				while (current != null)
				{
					foreach (FieldInfo field in current.GetFields(
						BindingFlags.Instance | BindingFlags.Public | 
						BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
					{
						if (!fields.ContainsKey(field.Name))
							fields[field.Name] = field;
					}
					current = current.BaseType;
				}
				cache[type] = fields;
			}

			fields.TryGetValue(fieldName, out var fi);
			return fi;
		}
	}
}