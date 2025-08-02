using UnityEngine;

namespace Modules.CustomAttribute 
{
	/// <summary>
	/// Clamp value between the two given value
	/// </summary>
	public class ClampAttribute : PropertyAttribute
	{
		public float Min { get; }
		public float Max { get; }

		public ClampAttribute(float min, float max)
		{
			Min = min;
			Max = max;
		}
	}
}