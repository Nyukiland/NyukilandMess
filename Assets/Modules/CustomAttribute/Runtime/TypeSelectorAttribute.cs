using UnityEngine;
using System;

namespace Modules.CustomAttribute
{
	public class TypeSelectorAttribute : PropertyAttribute
	{
		public Type Type { get; private set; }

		public TypeSelectorAttribute(Type type)
		{
			Type = type;
		}
	}
}
