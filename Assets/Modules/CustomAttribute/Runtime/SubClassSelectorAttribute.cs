using UnityEngine;
using System;

namespace Modules.CustomAttribute
{
	public class SubClassSelectorAttribute : PropertyAttribute
	{
		public Type BaseType { get; private set; }

		public SubClassSelectorAttribute(Type baseType)
		{
			BaseType = baseType;
		}
	}
}