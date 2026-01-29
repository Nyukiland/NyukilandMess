using System.Collections.Generic;
using UnityEngine;

namespace FolderColor
{
	public class FolderColorDatabase : ScriptableObject
	{
		public FolderColorDataControl.FolderColorMode Mode =FolderColorDataControl.FolderColorMode.Global;

		public List<FolderColorDataControl.FolderColorEntry> Entries = new();
	}
}