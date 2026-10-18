using Construction;
using AssetIndex.Core;
using InventorySystem;
using InventorySystem.Items;
using ObjectTags;
using Settlements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Interaction.ActorInteractables.Structures
{
    public class EnvironmentalBlueprint : BlueprintActorInteractableBase, IBlueprintObject
	{
		protected override int m_minActorsToFunction => 0;

		[Header("Settings")]
		[SerializeField] private ItemQuantity[] m_requiredItems;
		[SerializeField] private int m_blueprintSettlementID;

		[SerializeField] private BlueprintData m_blueprintToSpawnOnCompletion;
		[SerializeField] private GameObject m_objectEnabledOnCompletion;

		// IBlueprintObject properties
		public event Action<IBlueprintObject> BlueprintCompleted;
		public event Action<IBlueprintObject> BlueprintCanceled;

		protected override void Awake()
		{
			base.Awake();

			// Extract item tags from required items
			HashSet<ResourceTypeTag> uniqueTags = new HashSet<ResourceTypeTag>();
			if (m_requiredItems != null)
			{
				foreach (var requiredItem in m_requiredItems)
				{
					if (requiredItem.itemType is ITaggable<ResourceTypeTag> taggableItem && 
						taggableItem.RuntimeTagSet != null)
					{
						uniqueTags.UnionWith(taggableItem.RuntimeTagSet);
					}
				}
			}

			m_tagFilter = uniqueTags.ToArray();

			m_inventoryComponent.InitializeInventory(m_requiredItems.Length);
			m_resourceRequestComponent.SetRequiredItems(m_inventoryComponent.Inventory, m_requiredItems);
		}

		protected void Start()
		{
			m_settlementID = m_blueprintSettlementID;

			if (SettlementManager.s_WorldSettlements.TryGetValue(m_settlementID, out Settlement settlement))
			{
				settlement.AddStructure(this);
			}
			else
			{
				Debug.LogWarning("Tried to add an EnvironmentalBlueprint to " +
					"a settlement that does not exist", this);
			}
		}

		public override void HandleBlueprintCompleted()
		{
			if (m_objectEnabledOnCompletion != null)
			{
				m_objectEnabledOnCompletion.SetActive(true);
			}
			else if (m_blueprintToSpawnOnCompletion != null) // Try and spawn a prefab from blueprint data
			{
				Debug.Log($"A blueprint of SettlementBlueprintID:{m_settlementStructureID} " +
					$"was completed in settlement:{SettlementID}.");

				GameObject prefab = m_blueprintToSpawnOnCompletion.PrefabSpawnedOnCompletion;
				GameObject spawnedStructureObj = Instantiate(prefab, transform.position, transform.rotation);

				if (spawnedStructureObj.TryGetComponent(out IStructure builtStructure))
				{
					SettlementManager.s_WorldSettlements[SettlementID].AddStructure(builtStructure);
				}
				else
				{
					Debug.LogError($"Prefab {prefab.name} is missing an IStructure component!", this);
				}
			}

			gameObject.SetActive(false);

			Debug.Log($"A blueprint of SettlementBlueprintID:{m_settlementStructureID} " +
				$"was completed in settlement:{SettlementID}.");
			BlueprintCompleted?.Invoke(this);
		}

		public void HandleBlueprintCanceled()
		{
			Debug.Log($"A blueprint of SettlementBlueprintID:{m_settlementStructureID} " +
				$"was canceled in settlement:{SettlementID}.");

			foreach (InventorySlot slot in m_inventoryComponent.Slots)
			{
				slot.RemoveFromStack(slot.AmountInSlot, out var _, true, transform.position);
			}

			BlueprintCanceled?.Invoke(this);
		}

		public void HandleBlueprintPlaced(BlueprintData structureBlueprintData, Vector3 position, Quaternion rotation) { }

		public void HandlePointAdded(Vector3 position) { }

		public override void LostMinimumActors() { }
	}
}
