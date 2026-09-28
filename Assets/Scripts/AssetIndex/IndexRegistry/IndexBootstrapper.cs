using System.Collections.Generic;
using UnityEngine;

namespace AssetIndex.Core
{
	public static class IndexBootstrapper
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void InitializeIndices()
		{
			if (IndexRegistrySettings.Instance == null)
			{
				Debug.LogError("IndexRegistrySettings instance is missing.");
				return;
			}

			foreach (ScriptableObject index in IndexRegistrySettings.Instance.IndicesToRegister)
			{
				if (index == null)
					continue;

				if (index is IRegistrableAssetIndex registrableIndex)
				{
					registrableIndex.RegisterSelf();
				}
				else
				{
					Debug.LogError($"{index.name} does not implement IRegistrableIndex.");
				}
			}
		}
	}
}
