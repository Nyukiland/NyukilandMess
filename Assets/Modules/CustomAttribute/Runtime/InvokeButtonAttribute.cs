using System;
using UnityEngine;

namespace Modules.CustomAttribute 
{
	/// <summary>
	/// Displays a button in the Inspector
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
	public class InvokeButtonAttribute : Attribute
	{
		public string ButtonLabel { get; }

		public InvokeButtonAttribute(string name)
		{
			ButtonLabel = name;
		}

	}
}