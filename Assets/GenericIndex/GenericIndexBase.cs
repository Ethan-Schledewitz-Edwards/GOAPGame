using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GenericIndex
{
	public abstract class GenericIndexBase<T> : ScriptableObject, IRegistrableIndex where T : ScriptableObject, IIndexedAsset
	{
		public int AssetsInIndex => assets.Length;
		[field: SerializeField] protected T[] assets { get; private set; }

		private Dictionary<string, T> m_nameCache;

		public void RegisterSelf()
		{
			IndexRegistry.Register<T>(this);
		}

		public T GetIndexedAsset(int id)
		{
			if (id < 0 || id >= assets.Length) 
				return null;

			return 
				assets[id];
		}

		public T GetIndexedAsset(string assetName)
		{
			if (m_nameCache == null)
			{
				m_nameCache = new Dictionary<string, T>(assets.Length);
				foreach (var asset in assets)
				{
					if (asset != null)
					{
						m_nameCache[asset.name] = asset;
					}
				}
			}

			if (m_nameCache.TryGetValue(assetName, out T foundAsset))
			{
				return foundAsset;
			}

			Debug.LogError($"[GenericIndex] Asset named '{assetName}' could not be found in the {typeof(T).Name} index.");
			return null;
		}

		public T[] GetAllIndexedAssets()
		{
			return assets;
		}

#if UNITY_EDITOR
		public void PopulateUniqueAssets()
		{
			string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}");
			List<T> newAssets = new List<T>();
			List<T> currentAssets = assets != null ? new List<T>(assets) : new List<T>();

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

			assets = currentAssets.ToArray();
			AssignNewIDs();
		}

		public void AssignNewIDs()
		{
			int updatedCount = 0;
			for (int i = 0; i < assets.Length; i++)
			{
				if (assets[i] == null) 
					continue;

				assets[i].SetID(i);
				UnityEditor.EditorUtility.SetDirty(assets[i]);
				updatedCount++;
			}

			UnityEditor.EditorUtility.SetDirty(this);
			UnityEditor.AssetDatabase.SaveAssets();

			Debug.Log($"<color=yellow>{typeof(T).Name} Index Updated!</color> Assigned IDs to {updatedCount} assets.");
		}
#endif
	}
}