using UnityEngine;

namespace Modules.CustomAttribute 
{
	/// <summary>
	/// Displays a button in the Inspector to invoke a method by name.
	/// 
	///	-	Use this on a private field of type `byte`
	///	-	Prefer marking the field with [SerializeField] for Unity serialization
	/// 
	/// !	This is recommended, yet not mandatory, to be in editor-only field	!
	/// </summary>
	public class InvokeButtonAttribute : PropertyAttribute
	{
		public string MethodName { get; }

		public InvokeButtonAttribute(string methodName)
		{
			MethodName = methodName;
		}

	}
}