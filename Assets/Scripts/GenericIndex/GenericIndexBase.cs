using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GenericIndex
{
	public abstract class GenericIndexBase<T> : ScriptableObject, IRegistrableIndex where T : ScriptableObject, IIndexedAsset
	{
		public int AssetsInIndex => assets.Length;
		[field: SerializeField] protected T[] assets { get; private set; }

		public void RegisterSelf()
		{
			IndexRegistry.Register<T>(this);
		}

		public T GetIndexedAsset(string assetKey)
		{
			if (assets == null)
				return null;

			T foundAsset = assets.FirstOrDefault(asset => asset != null && asset.ID == assetKey);

			if (foundAsset != null)
				return foundAsset;

			Debug.LogError($"[GenericIndex] Asset named '{assetKey}' could not be found in the {typeof(T).Name} index.");
			return null;
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
		}

		public void AssignAssetIDs()
		{
			PopulateUniqueAssets();

			if (assets == null)
				return;

			foreach (T asset in assets)
			{
				if (asset == null)
					continue;

				asset.SetID(asset.name);
				UnityEditor.EditorUtility.SetDirty(asset);
			}

			UnityEditor.EditorUtility.SetDirty(this);
			UnityEditor.AssetDatabase.SaveAssets();
		}
#endif
	}
}