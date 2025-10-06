using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using WeaponData;

namespace WeaponDataEditor
{
	public class WeaponDataWindow : EditorWindow
	{
		private bool _weaponMode = false;

		private List<WeaponDataScriptable> _allWeaponDatas;
		private List<WeaponDataListScriptable> _allWeaponDataList;

		public WeaponDataScriptable CurrentEditedWeaponData;
		public WeaponDataListScriptable CurrentEditedWeaponDataList;

		private Vector2 _scrollPos;
		private Vector2 _listScrollPos;

		private string _nameFilter;
		private HashSet<Rarity> _rarityFilters = new();
		private bool _includeRanged = true;
		private bool _includeMelee = true;

		private readonly string[] _sortOptions = { "Name", "Price", "Power", "Rarity" };
		private int _selectedSortIndex = 0;

		private HashSet<WeaponDataScriptable> _selected = new();
		private string _newListName;

		private Editor _listInspectorEditor;

		[MenuItem("Tools/WeaponData Manager")]
		public static void ShowWindow()
		{
			WeaponDataWindow window = GetWindow<WeaponDataWindow>();
			window.titleContent = new GUIContent("WeaponData Manager");
			window.Show();
		}

		public static void ShowWindow(WeaponDataListScriptable weaponDataList)
		{
			WeaponDataWindow window = GetWindow<WeaponDataWindow>();
			window.titleContent = new GUIContent("WeaponData Manager");
			window.Show();
			window.CurrentEditedWeaponDataList = weaponDataList;
		}

		private void OnEnable()
		{
			RefreshData();
		}

		private void RefreshData()
		{
			_allWeaponDatas = AssetDatabase.FindAssets("t:WeaponDataScriptable")
				.Select(guid => AssetDatabase.LoadAssetAtPath<WeaponDataScriptable>(AssetDatabase.GUIDToAssetPath(guid)))
				.ToList();

			_allWeaponDataList = AssetDatabase.FindAssets("t:WeaponDataListScriptable")
				.Select(guid => AssetDatabase.LoadAssetAtPath<WeaponDataListScriptable>(AssetDatabase.GUIDToAssetPath(guid)))
				.ToList();
		}

		private void OnGUI()
		{
			GUILayout.Space(20);
			GUIStyle titleStyle = new()
			{
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleCenter,
				fontSize = 24,
				normal = { textColor = Color.white }
			};
			GUILayout.Label("WeaponData Manager", titleStyle);
			GUILayout.Space(20);

			// Toolbar
			GUILayout.BeginHorizontal(EditorStyles.toolbar);
			if (GUILayout.Toggle(_weaponMode, "Weapon Data", EditorStyles.toolbarButton))
				_weaponMode = true;

			if (GUILayout.Toggle(!_weaponMode, "Lists", EditorStyles.toolbarButton))
				_weaponMode = false;
			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(80)))
				RefreshData();

			GUILayout.EndHorizontal();

			GUILayout.Space(10);

			if (_weaponMode)
				DrawWeaponDataView();
			else
				DrawListView();
		}

		#region WeaponData
		private void DrawWeaponDataView()
		{
			EditorGUILayout.LabelField("Filter Options", EditorStyles.boldLabel);

			GUILayout.BeginHorizontal();
			_nameFilter = EditorGUILayout.TextField("Name Filter", _nameFilter);
			GUILayout.Space(50); GUILayout.Label("Sort by: ", GUILayout.Width(80));
			_selectedSortIndex = EditorGUILayout.Popup("", _selectedSortIndex, _sortOptions, GUILayout.MaxWidth(200));
			GUILayout.EndHorizontal();

			EditorGUILayout.Separator();

			GUILayout.BeginHorizontal();
			GUILayout.Space(50);

			GUILayout.BeginVertical();
			EditorGUILayout.LabelField("Rarity Filters:");
			foreach (Rarity rarity in System.Enum.GetValues(typeof(Rarity)))
			{
				bool selected = _rarityFilters.Contains(rarity);
				bool newSelected = EditorGUILayout.ToggleLeft(rarity.ToString(), selected);
				if (newSelected && !selected)
					_rarityFilters.Add(rarity);
				else if (!newSelected && selected)
					_rarityFilters.Remove(rarity);
			}
			GUILayout.EndVertical();
			EditorGUILayout.Space();

			GUILayout.BeginVertical();
			EditorGUILayout.LabelField("Weapon Type Filters:", EditorStyles.boldLabel);
			_includeMelee = EditorGUILayout.ToggleLeft("Include Melee", _includeMelee);
			_includeRanged = EditorGUILayout.ToggleLeft("Include Ranged", _includeRanged);
			GUILayout.EndVertical();

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUILayout.Space(15);
			EditorGUILayout.LabelField("Weapon List", EditorStyles.boldLabel);
			GUILayout.Space(5);

			var filtered = _allWeaponDatas
				.Where(w => string.IsNullOrEmpty(_nameFilter) || w.WeaponName.ToLower().Contains(_nameFilter.ToLower()))
				.Where(w => _rarityFilters.Count == 0 || _rarityFilters.Contains(w.Rarity))
				.Where(w => (_includeRanged && w.IsRanged) || (_includeMelee && !w.IsRanged))
				.ToList();

			filtered = _selectedSortIndex switch
			{
				0 => filtered.OrderBy(w => w.WeaponName).ToList(),
				1 => filtered.OrderBy(w => w.Price).ToList(),
				2 => filtered.OrderBy(w => w.Power).ToList(),
				3 => filtered.OrderBy(w => w.Rarity).ToList(),
				_ => filtered
			};

			_scrollPos = GUILayout.BeginScrollView(_scrollPos);
			foreach (var weapon in filtered)
			{
				GUILayout.BeginVertical(EditorStyles.helpBox);
				GUILayout.BeginHorizontal();

				GUILayout.BeginVertical();
				EditorGUILayout.LabelField(weapon.WeaponName, EditorStyles.boldLabel);
				EditorGUILayout.LabelField($"Price: {weapon.Price} | Power: {weapon.Power} | Rarity: {weapon.Rarity} | {(weapon.IsRanged ? "Ranged" : "Melee")}");
				EditorGUILayout.ObjectField("Asset", weapon, typeof(WeaponDataScriptable), false);
				GUILayout.EndVertical();

				bool isSelected = _selected.Contains(weapon);
				bool newToggle = GUILayout.Toggle(isSelected, GUIContent.none, GUILayout.Width(20), GUILayout.Height(50));
				if (newToggle != isSelected)
				{
					if (newToggle)
						_selected.Add(weapon);
					else
						_selected.Remove(weapon);
				}

				GUILayout.EndHorizontal();
				GUILayout.EndVertical();
			}
			GUILayout.EndScrollView();

			if (_selected.Count > 0)
			{
				GUILayout.Space(15);
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.BeginVertical(GUILayout.Width(position.width * 0.9f));
				EditorGUILayout.LabelField("Create New List", EditorStyles.boldLabel);
				_newListName = EditorGUILayout.TextField("List Name", _newListName);
				if (GUILayout.Button("Create List", GUILayout.Height(25)))
				{
					if (!string.IsNullOrEmpty(_newListName))
						CreateWeaponListAsset();
				}

				GUI.color = new(1f, 0f, 0f, 0.5f);
				if (GUILayout.Button("Clear Selection", GUILayout.Height(20)))
				{
					_newListName = "";
					_selected.Clear();
				}
				GUILayout.EndVertical();
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
			}
		}
		#endregion

		#region List View
		private void DrawListView()
		{
			EditorGUILayout.LabelField("Weapon List Scriptables", EditorStyles.boldLabel);
			EditorGUILayout.Space(5);

			_listScrollPos = EditorGUILayout.BeginScrollView(_listScrollPos);
			foreach (var list in _allWeaponDataList)
			{
				bool isSelected = CurrentEditedWeaponDataList == list;
				GUIStyle boxStyle = new(EditorStyles.helpBox);
				if (isSelected)
					boxStyle.normal.background = Texture2D.grayTexture;

				EditorGUILayout.BeginVertical(boxStyle);
				GUILayout.BeginHorizontal();

				EditorGUILayout.ObjectField(list, typeof(WeaponDataListScriptable), false);

				if (GUILayout.Button("View", GUILayout.Width(60)))
				{
					CurrentEditedWeaponDataList = list;
					if (_listInspectorEditor != null)
						DestroyImmediate(_listInspectorEditor);
					_listInspectorEditor = Editor.CreateEditor(CurrentEditedWeaponDataList);
				}

				GUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();
			}
			EditorGUILayout.EndScrollView();

			if (CurrentEditedWeaponDataList != null)
			{
				GUILayout.Space(10);
				EditorGUILayout.LabelField($"Inspecting: {CurrentEditedWeaponDataList.name}", EditorStyles.boldLabel);
				EditorGUILayout.Space(5);

				if (_listInspectorEditor != null)
				{
					EditorGUI.BeginDisabledGroup(true);
					_listInspectorEditor.OnInspectorGUI();
					EditorGUI.EndDisabledGroup();
				}

				GUILayout.Space(10);
				if (GUILayout.Button("Modify This List", GUILayout.Height(30)))
				{
					PrepareListForEditing(CurrentEditedWeaponDataList);
					_weaponMode = true;
				}
			}
		}
		#endregion

		private void CreateWeaponListAsset()
		{
			WeaponDataListScriptable existing = _allWeaponDataList.FirstOrDefault(x => x.name == _newListName);
			if (existing != null)
			{
				var field = typeof(WeaponDataListScriptable).GetField("_weaponDatas",
					System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

				field.SetValue(existing, _selected.ToList());
				EditorUtility.SetDirty(existing);

			}
			else
			{
				string[] guids = AssetDatabase.FindAssets($"{nameof(WeaponDataWindow)} t:script");
				string path = AssetDatabase.GUIDToAssetPath(guids[0]);
				path = path.Replace($"Editor/{nameof(WeaponDataWindow)}.cs", $"Data/WeaponDataList/{_newListName}.asset");

				WeaponDataListScriptable newList = new(_newListName, _selected.ToList());
				AssetDatabase.CreateAsset(newList, path);
			}

			AssetDatabase.SaveAssets();

			_selected.Clear();
			_newListName = "";
			RefreshData();
		}

		private void PrepareListForEditing(WeaponDataListScriptable list)
		{
			_selected.Clear();
			foreach (var weapon in list.WeaponDatas)
				_selected.Add(weapon);

			_newListName = list.name;
		}
	}
}