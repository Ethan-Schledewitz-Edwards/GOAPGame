using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GenericIndex
{
	public abstract class GenericIndexBase<T> : ScriptableObject where T : ScriptableObject, IIndexedAsset
	{
		[field: SerializeField] public T[] Assets { get; private set; }

#if UNITY_EDITOR
		public void PopulateUniqueAssets()
		{
			string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}");
			List<T> newAssets = new List<T>();
			List<T> currentAssets = Assets != null ? new List<T>(Assets) : new List<T>();

			// Gather unique assets
			foreach (string guid in guids)
			{
				string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
				T data = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
				if (data != null && !currentAssets.Contains(data))
				{
					newAssets.Add(data);
				}
			}

			newAssets = newAssets.OrderBy(a => a.name).ToList();

			// Fill in gaps
			int newAssetIndex = 0;
			for (int i = 0; i < currentAssets.Count; i++)
			{
				if (currentAssets[i] == null && newAssetIndex < newAssets.Count)
				{
					currentAssets[i] = newAssets[newAssetIndex];
					newAssetIndex++;
				}
			}

			while (newAssetIndex < newAssets.Count)
			{
				currentAssets.Add(newAssets[newAssetIndex]);
				newAssetIndex++;
			}

			Assets = currentAssets.ToArray();
			AssignNewIDs();
		}

		public void AssignNewIDs()
		{
			int updatedCount = 0;
			for (int i = 0; i < Assets.Length; i++)
			{
				if (Assets[i] == null) 
					continue;

				Assets[i].SetID(i);
				UnityEditor.EditorUtility.SetDirty(Assets[i]);
				updatedCount++;
			}

			UnityEditor.EditorUtility.SetDirty(this);
			UnityEditor.AssetDatabase.SaveAssets();

			Debug.Log($"<color=yellow>{typeof(T).Name} Index Updated!</color> Assigned IDs to {updatedCount} assets.");
		}
#endif
	}
}