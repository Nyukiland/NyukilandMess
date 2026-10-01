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
		public IconColorMix(string guid, string path, Color color)
		{
			Guid = guid;
			IconPath = path;
			IconColor = color;
		}
		
		public string Guid;
		public string IconPath;
		public Color IconColor;
	}
}