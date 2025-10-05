using System.Collections.Generic;
using UnityEngine;

namespace WeaponData
{
	[CreateAssetMenu(fileName = "WeaponDataScriptable", menuName = "Scriptable Objects/WeaponData/WeaponData List")]
	public class WeaponDataListScriptable : ScriptableObject
	{
		public WeaponDataListScriptable(string name, List<WeaponDataScriptable> weaponDatas)
		{
			_listName = name;
			_weaponDatas = weaponDatas;
		}

		[SerializeField]
		private string _listName;

		[SerializeField]
		private List<WeaponDataScriptable> _weaponDatas = new();

		public string ListName => _listName;
		public List<WeaponDataScriptable> WeaponDatas => _weaponDatas;
	}
}