using UnityEngine;

namespace Modules.CustomAttribute 
{
	/// <summary>
	/// Display a box of information above the variable
	/// </summary>
	public class InfoBoxAttribute : PropertyAttribute
	{
		public string Message { get; }

		public InfoBoxAttribute(string message)
		{
			Message = message;
		}
	}
}