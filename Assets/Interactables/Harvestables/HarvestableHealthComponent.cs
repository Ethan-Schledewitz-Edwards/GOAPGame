using BehaviourTrees;
using InventorySystem;
using InventorySystem.Items;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
	private List<IItemObject> m_droppedItems = new List<IItemObject>();

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

				if(spawnedItem.TryGetComponent(out IItemObject item))
				{
					// Track the dropped item
					m_droppedItems.Add(item);
					item.ItemPickedUp += OnItemPickedUp;
				}
			}
		}
	}

	private void OnItemPickedUp(Transform itemTransform)
	{
		if(itemTransform != null && itemTransform.TryGetComponent(out IItemObject itemObject))
		{
			if (m_droppedItems.Contains(itemObject))
				m_droppedItems.Remove(itemObject);
		}
	}

	private void AssignDroppedItems()
	{
		// Find nearby actors
		Collider[] hitColliders = Physics.OverlapSphere(transform.position,
				c_assignRange,
				m_actorLayerMask,
				QueryTriggerInteraction.Collide);

		HashSet<IInteractor> interactors = new HashSet<IInteractor>();
		for (int i = 0; i < m_droppedItems.Count; i++)
		{
			IItemObject item = m_droppedItems[i];

			// Skip null items
			if (item != null && item.Transform.TryGetComponent(out InteractableObjectBase interactableObjectBase))
			{
				foreach (Collider hitCollider in hitColliders)
				{
					if (hitCollider.TryGetComponent(out IInteractor actor))
					{
						if (interactors.Contains(actor))
							continue;

						if (actor.Transform.TryGetComponent(out BehaviourTreeExecutorBase btExecutor))
						{
							AIContext aiContext = btExecutor.AIContext;
							Transform agentsTarget = aiContext.GetData<Transform>("TargetTransform");

							// Ensure the actor was responsible for destroying this harvestable
							if (transform == agentsTarget)
							{
								interactableObjectBase.TryInteract(actor, true);
								interactors.Add(actor);
								break;
							}
						}
					}
				}
			}
		}
	}
}
