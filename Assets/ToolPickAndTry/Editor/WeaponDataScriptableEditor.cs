using System.Reflection;
using UnityEditor;
using UnityEngine;
using WeaponData;

namespace WeaponDataEditor
{
	[CustomEditor(typeof(WeaponDataScriptable))]
	[CanEditMultipleObjects]
	public class WeaponDataScriptableEditor : Editor
	{
		private WeaponDataScriptable _weaponData;

		SerializedProperty _idProp;
		SerializedProperty _weaponNameProp;
		SerializedProperty _priceProp;
		SerializedProperty _powerProp;
		SerializedProperty _isRangedProp;
		SerializedProperty _rarityProp;

		private void OnEnable()
		{
			_weaponData = (WeaponDataScriptable)target;

			_idProp = serializedObject.FindProperty("_id");
			_weaponNameProp = serializedObject.FindProperty("_weaponName");
			_priceProp = serializedObject.FindProperty("_price");
			_powerProp = serializedObject.FindProperty("_power");
			_isRangedProp = serializedObject.FindProperty("_isRanged");
			_rarityProp = serializedObject.FindProperty("_rarity");

		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			GUILayout.Space(10);

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(_weaponNameProp);
			EditorGUILayout.PropertyField(_priceProp);

			EditorGUILayout.PropertyField(_powerProp);
			EditorGUILayout.PropertyField(_isRangedProp);
			EditorGUILayout.PropertyField(_rarityProp);

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(_weaponData, "Change Value");

				var method = typeof(WeaponDataScriptable).GetMethod("GenerateID",BindingFlags.NonPublic | BindingFlags.Instance);
				method?.Invoke(_weaponData, null);

				_idProp.intValue = _weaponData.ID;
				_weaponData.name = _weaponData.WeaponName;

				EditorUtility.SetDirty(_weaponData);
				Repaint();
				AssetDatabase.SaveAssets();
			}

			GUILayout.FlexibleSpace();

			GUI.enabled = false;
			EditorGUILayout.PropertyField(_idProp);
			GUI.enabled = true;

			serializedObject.ApplyModifiedProperties();
		}
	}
}