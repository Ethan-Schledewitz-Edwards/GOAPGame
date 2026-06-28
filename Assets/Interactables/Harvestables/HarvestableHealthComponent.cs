using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;
using InventorySystem.Items;

public class HarvestableHealthComponent : HealthComponent
{
	// Constants
	private const float c_assignRange = 5.0f;

	[Header("Loot")]
	[SerializeField] private ItemTable m_itemTable;
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
		if(m_meshRenderer == null )
			m_meshRenderer = GetComponentInChildren<MeshRenderer>();

		m_actorLayerMask = 1 << LayerMask.NameToLayer("Actor");

		m_rb.useGravity = false;
		m_rb.isKinematic = true;
	}

	protected override void Start(){}

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
		if (m_itemTable != null)
		{
			GameObject lootToDrop = m_itemTable.GetRandomLoot(isLootGuaranteed);

			// Spawn the generated loot
			if (lootToDrop != null)
			{
				GameObject spawnedItem = Instantiate(lootToDrop, null);
				spawnedItem.transform.position = pos;

				if(spawnedItem.TryGetComponent(out Item item))
				{
					// Track the dropped item
					m_droppedItems.Add(item);
					item.ItemPickedUp += OnItemPickedUp;
				}
			}
		}
	}

	private void OnItemPickedUp(Item item)
	{
		if(m_droppedItems.Contains(item))
			m_droppedItems.Remove(item);
	}

	private void AssignDroppedItems()
	{
		// Find nearby actors
		Collider[] hitColliders = Physics.OverlapSphere(transform.position,
				c_assignRange,
				m_actorLayerMask,
				QueryTriggerInteraction.Collide);

		HashSet<IInteractor> interactors = new HashSet<IInteractor>();
		for (int i = 0;i < m_droppedItems.Count; i++)
		{
			Item item = m_droppedItems[i];

			// Skip null items
			if(item == null) 
				continue;

			foreach (Collider hitCollider in hitColliders)
			{
				if (hitCollider.TryGetComponent(out IInteractor actor))
				{
					if (interactors.Contains(actor))
						continue;

					if(actor.Transform.TryGetComponent(out BehaviourTreeExecutor btExecutor))
					{
						AIContext aiContext = btExecutor.AIContext;
						Transform agentsTarget = aiContext.GetData<Transform>("TargetTransform");

						// Ensure the actor was responsible for destroying this harvestable
						if (transform == agentsTarget)
						{
							item.TryInteract(actor);
							interactors.Add(actor);
							break;
						}
					}
				}
			}
		}
	}
}
