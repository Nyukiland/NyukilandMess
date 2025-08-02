using UnityEngine;
using System;

namespace Modules.CustomAttribute
{
	/// <summary>
	/// Display a drawer of given type
	/// </summary>
	public class TypeSelectorAttribute : PropertyAttribute
	{
		public Type Type { get; }

		public TypeSelectorAttribute(Type type)
		{
			Type = type;
		}
	}
}
