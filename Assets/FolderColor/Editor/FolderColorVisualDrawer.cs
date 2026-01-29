using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace FolderColor
{
	[InitializeOnLoad]
	public class FolderColorVisualDrawer
    {
		static FolderColorVisualDrawer()
		{
			EditorApplication.projectWindowItemOnGUI += OnGUI;
		}

		private static void OnGUI(string guid, Rect rect)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (!AssetDatabase.IsValidFolder(path)) return;

			DrawForDataBase(guid, rect, FolderColorDataControl.FolderColorMode.Global);
			DrawForDataBase(guid, rect, FolderColorDataControl.FolderColorMode.Personal);
		}

		private static void DrawForDataBase(string guid, Rect rect, FolderColorDataControl.FolderColorMode mode)
		{
			var entry = FolderColorDataControl.FolderColorData.Get(guid, mode);
			if (entry == null) return;

			Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(entry.IconPath);
			if (icon == null)
			{
				FolderColorDataControl.FolderColorData.Remove(guid);
				return;
			}

			Rect iconRect = rect.height <= 18
				? new Rect(rect.x + 2, rect.y, rect.height, rect.height)
				: new Rect(rect.x, rect.y, rect.height - 8, rect.height - 8);

			GUI.DrawTexture(iconRect, icon);
		}
	}
}