using UnityEngine;
using System;

namespace Modules.CustomAttribute
{
	/// <summary>
	/// A more complexed `TypeSelector`
	/// Allows to choose a class and control the var in it
	/// 
	///	!	Requires `SerializeReference`	!
	/// </summary>
	public class SubClassSelectorAttribute : PropertyAttribute
	{
		public Type BaseType { get; }

		public SubClassSelectorAttribute(Type baseType)
		{
			BaseType = baseType;
		}
	}
}