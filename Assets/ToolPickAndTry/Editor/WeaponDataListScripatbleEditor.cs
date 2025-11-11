using UnityEditor;
using UnityEngine;
using WeaponData;

namespace WeaponDataEditor
{
	[CustomEditor(typeof(WeaponDataListScriptable))]
	[CanEditMultipleObjects]
	public class WeaponDataListScripatbleEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (GUILayout.Button("Modify List In Window"))
			{
				WeaponDataWindow.ShowWindow(target as WeaponDataListScriptable);
			}
		}
	}
}