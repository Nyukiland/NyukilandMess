using Modules.CustomAttribute;
using UnityEngine;
using System;
using System.Security.Cryptography;
using System.Text;

namespace WeaponData
{
	[CreateAssetMenu(fileName = "WeaponDataScriptable", menuName = "Scriptable Objects/WeaponData/WeaponData")]
	public class WeaponDataScriptable : ScriptableObject
	{
		[SerializeField]
		[Disable]
		private int _id;

		[SerializeField]
		private string _weaponName;

		[SerializeField]
		private int _price;

		[SerializeField]
		private float _power;

		[SerializeField]
		private bool _isRanged;

		[SerializeField]
		private Rarity _rarity;

		public int ID
		{
			get
			{
				if (_id == 0)
				{
					GenerateID();
				}
				return _id;
			}
		}

		public string WeaponName => _weaponName;
		public int Price => _price;
		public float Power => _power;
		public bool IsRanged => _isRanged;
		public Rarity Rarity => _rarity;

		private void GenerateID()
		{
			string combined = $"{_weaponName}_{_price}_{_power}_{_isRanged}_{_rarity}";
			using (SHA1 sha = SHA1.Create())
			{
				byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
				_id = BitConverter.ToInt32(hash, 0);
			}
		}
	}

	[Serializable]
	public enum Rarity
	{
		Common = 0,
		Rare = 1,
		Epic = 2,
		Legendary = 3
	}
}