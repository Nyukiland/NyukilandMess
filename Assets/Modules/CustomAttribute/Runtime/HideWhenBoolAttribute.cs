using UnityEngine;

namespace Modules.CustomAttribute
{
	/// <summary>
	/// Given a boolean as parameter
	/// It will show/hide the var depending on the boolean value
	/// </summary>
	public class HideWhenBoolAttribute : PropertyAttribute
	{
		public string PropertyString { get; }
		public bool PropertyBool { get; }

		public HideWhenBoolAttribute(string varName, bool visibleWhen = true)
		{
			PropertyString = varName;
			PropertyBool = visibleWhen;
		}
	}
}