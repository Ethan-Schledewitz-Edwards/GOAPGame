using System;
using System.Collections.Generic;
using UnityEngine;

namespace AssetIndex.Core
{
    public static class IndexRegistry
    {
		private static readonly Dictionary<Type, object> s_registryMap = new();

		public static void Register<T>(AssetIndexBase<T> indexAsset) where T : ScriptableObject, IIndexedAsset
		{
			Type assetType = typeof(T);

			if (s_registryMap.ContainsKey(assetType))
			{
				Debug.LogWarning($"[IndexRegistry] Index for type {assetType.Name} is already registered.");
				return;
			}

			s_registryMap[assetType] = indexAsset;
			Debug.Log($"<color=green>[IndexRegistry] Successfully bound {assetType.Name} Index!</color>");
		}

		public static AssetIndexBase<T> GetIndex<T>() where T : ScriptableObject, IIndexedAsset
		{
			if (s_registryMap.TryGetValue(typeof(T), out var index))
			{
				return (AssetIndexBase<T>)index;
			}

			Debug.LogError($"[IndexRegistry] No index registered for type {typeof(T).Name}. Did you forget to initialize it?");
			return null;
		}

		public static T GetAsset<T>(string assetID) where T : ScriptableObject, IIndexedAsset
		{
			var index = GetIndex<T>();
			if (index == null)
				return null;

			return index.GetIndexedAsset(assetID);
		}
	}
}
