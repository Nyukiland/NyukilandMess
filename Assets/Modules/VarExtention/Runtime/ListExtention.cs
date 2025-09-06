using System.Collections.Generic;
using UnityEngine;

namespace Modules.VarExtention
{
	public static class ListExtention
	{
		/// <summary>
		/// Returns a random element from the list
		/// </summary>
		public static T GetRandom<T>(this List<T> list)
		{
			if (list == null || list.Count == 0)
			{
				Debug.LogWarning("Trying to get a random element from an empty or null list!");
				return default;
			}

			int index = Random.Range(0, list.Count);
			return list[index];
		}

		/// <summary>
		/// Returns a random element from the list and remove it
		/// </summary>
		public static T GetRandomAndRemove<T>(this List<T> list)
		{
			if (list == null || list.Count == 0)
			{
				Debug.LogWarning("Trying to get a random element from an empty or null list!");
				return default;
			}

			int index = Random.Range(0, list.Count);
			T element = list[index];
			list.RemoveAt(index);
			return element;
		}

		/// <summary>
		/// Shuffles a list in place
		/// </summary>
		public static void Shuffle<T>(this List<T> list, int iteration = -1)
		{
			if (list == null || list.Count <= 1) return;

			if (iteration < 0) iteration =list.Count;

			for (int i = 0; i < iteration; i++)
			{
				int indexA = Random.Range(0, list.Count);
				int indexB = Random.Range(0, list.Count);

				if (indexA == indexB) continue;

				T temp = list[indexA];
				list[indexA] = list[indexB];
				list[indexB] = temp;
			}
		}
	}
}