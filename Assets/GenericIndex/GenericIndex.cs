using System.Collections.Generic;
using UnityEngine;

namespace GenericIndex
{
	public abstract class GenericIndex<T> : ScriptableObject where T : ScriptableObject, IIndexedAsset
	{
		[field: SerializeField] public T[] Assets { get; private set; }

#if UNITY_EDITOR
		public void PopulateAndAssignIDs()
		{
			// Use the type name dynamically to find the matching files
			string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}");
			List<T> foundAssets = new List<T>();

			foreach (string guid in guids)
			{
				string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
				T data = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);

				if (data != null)
				{
					foundAssets.Add(data);
				}
			}
			Assets = foundAssets.ToArray();

			// Sequence IDs
			int updatedCount = 0;
			for (int i = 0; i < Assets.Length; i++)
			{
				Assets[i].SetID(i);
				UnityEditor.EditorUtility.SetDirty(Assets[i]);
				updatedCount++;
			}

			UnityEditor.EditorUtility.SetDirty(this);
			UnityEditor.AssetDatabase.SaveAssets();

			Debug.Log($"<color=yellow>{typeof(T).Name} Index Updated!</color> Found and assigned IDs to {updatedCount} assets.");
		}
#endif
	}
}