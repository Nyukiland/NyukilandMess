using UnityEngine;

namespace Modules.CustomAttribute
{
	public class ChangeColorAttribute : PropertyAttribute
	{
		public Color Color { get; private set; }

		ChangeColorAttribute(string hexaDecimal)
		{
			if (ColorUtility.TryParseHtmlString(hexaDecimal, out Color color))
				Color = color;
			else
				Color = Color.white;
		}

		ChangeColorAttribute(float r, float g, float b)
		{
			Color = new Color(r, g, b);
		}
	}
}