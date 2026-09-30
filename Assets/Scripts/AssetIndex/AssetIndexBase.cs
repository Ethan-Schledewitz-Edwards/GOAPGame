using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
	NOTE:
	I've read more of the code now, and I think I understand why you've used string IDs so much in
	this project. It's probably becuase of assembly definitions, right? I think you've been
	overusing them. The intended purpose of an assembly definition is only to allow you to
	compile sections of the project separately. It's an optimization. Prevent cyclic dependencies
	is only a side effect. But it's a side effect that forces you to write understandable, simpler
	code. However, there is a point where a cyclic dependency is necessary for something to even
	function. Rather than using strings when you reach that point (which is just a hacky way of getting
	around that limitation, and only creates the illusion of a linear dependency tree), just merge the
	assemblies into one. This gives you access to strong typing	and will in general make things more
	robust. I think you should only use an assembly when two systems are actually independent.

	Anyways, some of the string IDs you're using are actually warrented, but in those cases I'd replace
	them with the file name or hash so you don't have to generate IDs.
*/

namespace AssetIndex.Core
{
	public abstract class AssetIndexBase<T> : ScriptableObject, IRegistrableAssetIndex where T : ScriptableObject, IIndexedAsset
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

			Debug.LogError($"[GenericIndex] Asset named '{assetKey}' " +
				$"could not be found in the {typeof(T).Name} index.");
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
				if (currentAssets[i] == null &&
					newAssetIndex < newAssets.Count)
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