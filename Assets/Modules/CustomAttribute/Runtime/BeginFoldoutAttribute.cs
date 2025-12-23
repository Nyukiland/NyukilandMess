using UnityEngine;

namespace Modules.CustomAttribute 
{
	/// <summary>
	/// Given a name it will group in a foldout
	/// This allow to close multiple var together
	/// </summary>
	public class BeginFoldoutAttribute : PropertyAttribute
	{
		public string Title { get; }
		public int TitleSize { get; }

		public BeginFoldoutAttribute(string varName, int size = 14)
		{
			Title = varName;
			TitleSize = size;
		}
	}

	/// <summary>
	/// End a foldout
	/// </summary>
	public class EndFoldoutAttribute : PropertyAttribute { }
}