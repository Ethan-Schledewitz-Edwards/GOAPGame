using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using BehaviourTrees;

public class HarvestableHealth : HealthComponent
{
	// Constants
	private const float k_assignRange = 5.0f;

	[Header("Loot")]
	[SerializeField] private LootTable m_lootTable;
	[SerializeField] private int m_minTableRollsOnDeath = 2;
	[SerializeField] private int m_maxtableRollsOnDeath = 4;

	// System
	public bool IsDamagable => m_isDamagable;
	[SerializeField] private bool m_isDamagable = true;

	private MeshRenderer m_meshRenderer;

	private int m_consecutiveHits;
	private List<Item> m_droppedItems = new List<Item>();

	private int m_actorLayerMask;

	protected override void Awake()
	{
		base.Awake();

		m_meshRenderer = GetComponent<MeshRenderer>();

		m_actorLayerMask = 1 << LayerMask.NameToLayer("Actor");
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

		// Drop loot
		int rand = Random.Range(m_minTableRollsOnDeath, m_maxtableRollsOnDeath);
		for (int i = 0; i < rand; i++)
		{
			SpawnLoot(meshCenter, true);
		}

		// Assign dropped loot to workers
		AssignDroppedItems();

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

				// Track the dropped item
				m_droppedItems.Add(spawnedItem);
				spawnedItem.OnPickup += RemoveTrackedItem;
			}
		}
	}

	private void RemoveTrackedItem(Item item)
	{
		if(m_droppedItems.Contains(item))
			m_droppedItems.Remove(item);
	}

	private void AssignDroppedItems()
	{
		// Find nearby actors
		Collider[] hitColliders = Physics.OverlapSphere(transform.position,
				k_assignRange,
				m_actorLayerMask,
				QueryTriggerInteraction.Collide);

		for (int i = 0;i < m_droppedItems.Count; i++)
		{
			Item item = m_droppedItems[i];

			// Skip null items
			if(item == null) 
				continue;

			if(hitColliders.Length > 0)
			{
				Collider hitCollider = hitColliders[i];

				// Assign actor to the dropped item
				if (hitCollider.TryGetComponent(out Actor actor))
				{
					BehaviourTree actorBT = actor.BehaviourTree;

					// Ensure the actor was responsible for destroying this harvestable
					if (actorBT != null && 
						actorBT.TryGetData("targetTransform", out object targetTransform) &&
						transform == (Transform)targetTransform)
					{
						Debug.Log("THIS SHOULD HAPPEN BRO");
						actor.SetTask(item);
					}
				}
			}
		}
	}
}
