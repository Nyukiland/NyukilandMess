using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

		private string _nameFilter;
		private Rarity? _rarityFilter = null;
		private bool? _isRangedFilter;
		private string[] _sortOptions = { "Name", "Price", "Power", "Rarity" };
		private int _selectedSortIndex = 0;

		private Dictionary<WeaponDataScriptable, bool> _selection = new();

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

			_selection = _allWeaponDatas.ToDictionary(x => x, x => false);
		}



		private void OnGUI()
		{
			GUILayout.Space(20);
			GUIStyle titleStyle = new();
			titleStyle.fontStyle = FontStyle.Bold;
			titleStyle.alignment = TextAnchor.MiddleCenter;
			titleStyle.fontSize = 24;
			titleStyle.normal.textColor = Color.white;
			GUILayout.Label("WeaponData Manager", titleStyle);
			GUILayout.Space(20);

			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			if (GUILayout.Toggle(_weaponMode, "Weapon Data", EditorStyles.toolbarButton))
				_weaponMode = true;

			if (GUILayout.Toggle(!_weaponMode, "Lists", EditorStyles.toolbarButton))
				_weaponMode = false;
			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(80)))
				RefreshData();

			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10);

			_scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

			if (_weaponMode)
				DrawWeaponDataView();
			else
				DrawListView();

			EditorGUILayout.EndScrollView();

		}

		private void DrawWeaponDataView()
		{
			EditorGUILayout.LabelField("Filter Options", EditorStyles.boldLabel);
			_nameFilter = EditorGUILayout.TextField("Name Filter", _nameFilter);

			_rarityFilter = (Rarity?)EditorGUILayout.EnumPopup("Rarity", _rarityFilter ?? Rarity.Common);
			if (GUILayout.Button(_rarityFilter.HasValue ? "Clear Rarity Filter" : "Filter by Rarity"))
				_rarityFilter = _rarityFilter.HasValue ? null : Rarity.Common;

			_isRangedFilter = EditorGUILayout.Toggle("Ranged Only", _isRangedFilter ?? false);
			if (GUILayout.Button(_isRangedFilter.HasValue ? "Clear Range Filter" : "Filter by Range"))
				_isRangedFilter = _isRangedFilter.HasValue ? null : false;

			GUILayout.Space(5);
			_selectedSortIndex = EditorGUILayout.Popup("Sort By", _selectedSortIndex, _sortOptions);

			GUILayout.Space(10);
			EditorGUILayout.LabelField("Weapon List", EditorStyles.boldLabel);

			var filtered = _allWeaponDatas
				.Where(w => string.IsNullOrEmpty(_nameFilter) || w.WeaponName.ToLower().Contains(_nameFilter.ToLower()))
				.Where(w => !_rarityFilter.HasValue || w.Rarity == _rarityFilter)
				.Where(w => !_isRangedFilter.HasValue || w.IsRanged == _isRangedFilter)
				.ToList();

			filtered = _selectedSortIndex switch
			{
				0 => filtered.OrderBy(w => w.WeaponName).ToList(),
				1 => filtered.OrderBy(w => w.Price).ToList(),
				2 => filtered.OrderBy(w => w.Power).ToList(),
				3 => filtered.OrderBy(w => w.Rarity).ToList(),
				_ => filtered
			};

			foreach (var weapon in filtered)
			{
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				EditorGUILayout.LabelField(weapon.WeaponName, EditorStyles.boldLabel);
				EditorGUILayout.LabelField($"Price: {weapon.Price} | Power: {weapon.Power} | Rarity: {weapon.Rarity} | {(weapon.IsRanged ? "Ranged" : "Melee")}");
				EditorGUILayout.ObjectField("Asset", weapon, typeof(WeaponDataScriptable), false);
				EditorGUILayout.EndVertical();
			}
		}

		private void DrawListView()
		{
			EditorGUILayout.LabelField("Weapon List Scriptables", EditorStyles.boldLabel);
			foreach (var list in _allWeaponDataList)
			{
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				EditorGUILayout.ObjectField(list, typeof(WeaponDataListScriptable), false);
				EditorGUILayout.EndVertical();
			}

			GUILayout.Space(10);
			EditorGUILayout.LabelField("Create New List", EditorStyles.boldLabel);

			// Filters reused
			_nameFilter = EditorGUILayout.TextField("Name Filter", _nameFilter);
			_rarityFilter = (Rarity?)EditorGUILayout.EnumPopup("Rarity", _rarityFilter ?? Rarity.Common);
			if (GUILayout.Button(_rarityFilter.HasValue ? "Clear Rarity Filter" : "Filter by Rarity"))
				_rarityFilter = _rarityFilter.HasValue ? null : Rarity.Common;

			GUILayout.Space(5);
			EditorGUILayout.LabelField("Select Weapons to Include:", EditorStyles.miniBoldLabel);

			var filtered = _allWeaponDatas
				.Where(w => string.IsNullOrEmpty(_nameFilter) || w.WeaponName.ToLower().Contains(_nameFilter.ToLower()))
				.Where(w => !_rarityFilter.HasValue || w.Rarity == _rarityFilter)
				.ToList();

			foreach (var weapon in filtered)
			{
				_selection[weapon] = EditorGUILayout.ToggleLeft($"{weapon.WeaponName} ({weapon.Rarity})", _selection[weapon]);
			}

			GUILayout.Space(10);
			if (GUILayout.Button("Create New List"))
			{
				CreateWeaponListAsset();
			}
		}

		private void CreateWeaponListAsset()
		{
			string name = "temp";

			var selectedWeapons = _selection.Where(kv => kv.Value).Select(kv => kv.Key).ToList();
			if (selectedWeapons.Count == 0)
			{
				return;
			}

			string[] guids = AssetDatabase.FindAssets($"{nameof(WeaponDataWindow)} t:script");
			string path = AssetDatabase.GUIDToAssetPath(guids[0]);
			path = path.Replace($"Editor/{nameof(WeaponDataWindow)}.cs", $"Data/WeaponDataList/{name}.asset");

			WeaponDataListScriptable newList = new(name, selectedWeapons);

			AssetDatabase.CreateAsset(newList, path);
			AssetDatabase.SaveAssets();

			RefreshData();
		}
	}
}