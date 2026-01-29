using UnityEngine;
using UnityEditor;
using System.Linq;

namespace FolderColor
{
    public class FolderColorSelectionWindow : EditorWindow
    {
		private string[] _targetGuids;
		private Texture2D[] _icons;

		private bool _useGlobal;

		[MenuItem("Assets/Custom Folder Icon", false, 100)]
		private static void Open()
		{
			Open(Selection.assetGUIDs);
		}

		[MenuItem("Assets/Custom Folder Icon", true)]
		private static bool Validate()
		{
			return Selection.assetGUIDs.Any(g =>
				AssetDatabase.IsValidFolder(AssetDatabase.GUIDToAssetPath(g)));
		}

		private static void Open(string[] guids)
		{
			FolderColorSelectionWindow win = GetWindow<FolderColorSelectionWindow>("Folder Selection");
			win._targetGuids = guids;
			win.LoadIcons();
			win.Show();
		}

		private void LoadIcons()
		{
			string path = AssetDatabase.FindAssets($"{nameof(FolderColorSelectionWindow)} t:script")
				.Select(AssetDatabase.GUIDToAssetPath)
				.First()
				.Replace($"/Editor/{nameof(FolderColorSelectionWindow)}.cs", "");

			_icons = AssetDatabase.FindAssets("t:Texture2D", new[] { path })
				.Select(g => AssetDatabase.LoadAssetAtPath<Texture2D>(
					AssetDatabase.GUIDToAssetPath(g)))
				.ToArray();
		}

		private void OnGUI()
		{
			FolderColorToolBar();

			GUILayout.Space(10);

			int buttonSize = 80;
			int padding = 4; 
			int buttonsPerRow = Mathf.Max(1, Mathf.FloorToInt(position.width / (buttonSize + padding)));

			for (int i = 0; i < _icons.Length; i++)
			{
				if (i % buttonsPerRow == 0)
					GUILayout.BeginHorizontal();

				if (GUILayout.Button(_icons[i], GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
				{
					string iconPath = AssetDatabase.GetAssetPath(_icons[i]);
					foreach (string guid in _targetGuids)
					{
						FolderColorDataControl.FolderColorData.Set(guid, iconPath, 
							_useGlobal? FolderColorDataControl.FolderColorMode.Global : FolderColorDataControl.FolderColorMode.Personal);
					}

					GUILayout.EndHorizontal();
					Close();
				}

				if (i % buttonsPerRow == buttonsPerRow - 1 || i == _icons.Length - 1)
					GUILayout.EndHorizontal();
			}
		}

		private void FolderColorToolBar()
		{
			GUILayout.BeginHorizontal(EditorStyles.toolbar);

			GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
			buttonStyle.margin = new RectOffset(0, 0, 0, 0);

			if (GUILayout.Button("None", buttonStyle, GUILayout.Width(80)))
			{
				foreach (var g in _targetGuids)
					FolderColorDataControl.FolderColorData.Remove(g);

				Close();
			}

			GUILayout.FlexibleSpace();

			GUI.backgroundColor = !_useGlobal ? Color.green : Color.white;
			if (GUILayout.Button("Personal", buttonStyle, GUILayout.Width(80)))
			{
				if (_useGlobal)
				{
					_useGlobal = false;
					Repaint();
				}
			}

			GUI.backgroundColor = _useGlobal ? Color.green : Color.white;
			if (GUILayout.Button("Global", buttonStyle, GUILayout.Width(80)))
			{
				if (!_useGlobal)
				{
					_useGlobal = true;
					Repaint();
				}
			}

			GUI.backgroundColor = Color.white;

			GUILayout.EndHorizontal();
		}
	}
}