using System;
using System.Collections.Generic;
using UnityEngine;

namespace FolderColor
{
	public class FolderColorDatabase : ScriptableObject
	{
		public FolderColorDataControl.FolderColorMode Mode = FolderColorDataControl.FolderColorMode.Global;
		public string UserId;

		public List<IconColorMix> IconEntries = new();
		
		[Header("Old")]
		public List<FolderColorDataControl.FolderColorEntry> Entries = new();
	}

	[Serializable]
	public class IconColorMix
	{
		public IconColorMix(string guid, string path, Color color, 
			bool highlightName = false, bool highlightChildren = false)
		{
			Guid = guid;
			IconPath = path;
			IconColor = color;
			HighlightName = highlightName;
			HighlightChildren = highlightChildren;
		}
		
		public string Guid;
		public string IconPath;
		public Color IconColor;
		public bool HighlightName;
		public bool HighlightChildren;
	}
}