using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "LootTable", menuName = "Items/LootTable")]
public class LootTable : ScriptableObject
{
	[System.Serializable]
	public class LootEntry
	{
		[field: SerializeField] public ItemData Item { get; private set; }
		[field: SerializeField] public float Weight { get; private set; }
	}

	[SerializeField] private LootEntry[] lootEntries;

	public Item GetRandomLoot(bool cannotReturnNull)
	{
		int maxAttempts = 10;
		int attempts = 0;

		// Ensure all negative weights become 1
		float[] clampedWeights = lootEntries.Select(p => p.Weight <= 0f ? 1f : p.Weight).ToArray();

		float totalWeight = clampedWeights.Sum();
		if (totalWeight <= 0f) return null;

		// Normalize weights
		float[] normalizedWeights = clampedWeights.Select(w => w / totalWeight).ToArray();

		while (attempts < maxAttempts)
		{
			float rand = Random.value;

			for (int i = 0; i < lootEntries.Length; i++)
			{
				if (rand < normalizedWeights[i])
				{
					if (lootEntries[i].Item != null || !cannotReturnNull)
					{
						return lootEntries[i].Item.ItemPrefab;
					}
				}
				rand -= normalizedWeights[i];
			}
			attempts++;
		}

		return null;
	}
}
