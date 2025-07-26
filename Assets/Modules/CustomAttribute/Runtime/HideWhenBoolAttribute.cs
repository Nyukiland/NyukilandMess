using UnityEngine;

namespace Modules.CustomAttribute
{
	public class HideWhenBoolAttribute : PropertyAttribute
	{
		public string PropertyString { get; private set; }
		public bool PropertyBool { get; private set; }

		public HideWhenBoolAttribute(string varName, bool visibleWhen = true)
		{
			PropertyString = varName;
			PropertyBool = visibleWhen;
		}
	}
}