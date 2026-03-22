using System.Globalization;
using UnityEngine;

public class HarvestableHealth : HealthComponent
{
	[Header("Loot")]
	[SerializeField] private LootTable m_lootTable;
	[SerializeField] private int m_minTableRollsOnDeath = 2;
	[SerializeField] private int m_maxtableRollsOnDeath = 4;

	// System
	public bool IsDamagable => m_isDamagable;
	[SerializeField] private bool m_isDamagable = true;

	MeshRenderer m_meshRenderer;

	int m_consecutiveHits;

	protected override void Awake()
	{
		base.Awake();

		m_meshRenderer = GetComponent<MeshRenderer>();
	}

	protected override void OnTakeDamage()
	{
		base.OnTakeDamage();

		m_consecutiveHits++;

		if (m_consecutiveHits >= 3)
		{
			Vector3 meshCenter = m_meshRenderer.bounds.center;
			SpawnLoot(meshCenter, true);
			m_consecutiveHits = 0;
		}
	}

	protected override void OnDie()
	{
		// Get the center of the harvestable
		Vector3 meshCenter = m_meshRenderer.bounds.center;

		int rand = Random.Range(m_minTableRollsOnDeath, m_maxtableRollsOnDeath);

		for (int i = 0; i < rand; i++)
		{
			SpawnLoot(meshCenter, true);
		}

		gameObject.SetActive(false);
	}

	private void SpawnLoot(Vector3 pos, bool isLootGuaranteed)
	{
		if (m_lootTable != null)
		{
			Item lootToDrop = m_lootTable.GetRandomLoot(isLootGuaranteed);

			// Spawn the generated loot
			if (lootToDrop != null)
			{
				Item spawnedItem = Instantiate(lootToDrop, null);
				spawnedItem.transform.position = pos;
			}
		}
	}
}
