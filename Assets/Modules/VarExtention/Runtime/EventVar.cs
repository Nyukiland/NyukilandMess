using System;
using UnityEngine;

namespace Modules.VarExtention
{
	[Serializable]
	public class EventVar<T>
	{
		[SerializeField]
		private T _value;

		public delegate void ValueChanged(T newValue);
		public ValueChanged OnValueChanged;

		public T Value
		{
			get => _value;
			set
			{
				OnValueChanged.Invoke(value);
				_value = value;
			}
		}
	}
}