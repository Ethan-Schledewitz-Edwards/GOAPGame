using GenericIndex;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GenericIndex
{
    public static class IndexRegistry
    {
		private static readonly Dictionary<Type, object> _registryMap = new();

		public static void Register<T>(GenericIndexBase<T> indexAsset) where T : ScriptableObject, IIndexedAsset
		{
			Type assetType = typeof(T);

			if (_registryMap.ContainsKey(assetType))
			{
				Debug.LogWarning($"[IndexRegistry] Index for type {assetType.Name} is already registered.");
				return;
			}

			_registryMap[assetType] = indexAsset;
			Debug.Log($"<color=green>[IndexRegistry] Successfully bound {assetType.Name} Index!</color>");
		}

		public static GenericIndexBase<T> GetIndex<T>() where T : ScriptableObject, IIndexedAsset
		{
			if (_registryMap.TryGetValue(typeof(T), out var index))
			{
				return (GenericIndexBase<T>)index;
			}

			Debug.LogError($"[IndexRegistry] No index registered for type {typeof(T).Name}. Did you forget to initialize it?");
			return null;
		}

		public static T GetAsset<T>(int id) where T : ScriptableObject, IIndexedAsset
		{
			var index = GetIndex<T>();
			if (index == null) 
				return null;

			// Safe boundary check
			if (id < 0 || id >= index.AssetsInIndex) 
				return null;

			return index.GetIndexedAsset(id);
		}

		public static T GetAsset<T>(string assetName) where T : ScriptableObject, IIndexedAsset
		{
			var index = GetIndex<T>();
			if (index == null)
				return null;

			return index.GetIndexedAsset(assetName);
		}
	}
}
