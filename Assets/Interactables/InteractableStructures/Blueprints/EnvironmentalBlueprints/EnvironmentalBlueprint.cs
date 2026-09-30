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

namespace Interaction.InteractableStructures.Blueprints
{
    public class EnvironmentalBlueprint : BlueprintIOBase, IBlueprintObject
	{
		[Header("Settings")]
		[SerializeField] private ItemQuantity[] m_requiredItems;
		[SerializeField] private int m_blueprintSettlementID;

		[SerializeField] private BlueprintData m_blueprintToSpawnOnCompletion;
		[SerializeField] private GameObject m_objectEnabledOnCompletion;

		public event Action<IBlueprintObject> BlueprintCompleted;
		public event Action<IBlueprintObject> BlueprintCanceled;

		protected override void Awake()
		{
			base.Awake();

			// Extract item tags from required items
			HashSet<ItemTag> uniqueTags = new HashSet<ItemTag>();
			if (m_requiredItems != null)
			{
				foreach (var requiredItem in m_requiredItems)
				{
					if (requiredItem.itemType is ITaggable<ItemTag> taggableItem && 
						taggableItem.RuntimeTagSet != null)
					{
						uniqueTags.UnionWith(taggableItem.RuntimeTagSet);
					}
				}
			}

			m_tagFilter = uniqueTags.ToArray();

			m_inventoryComponent.InitializeInventory(m_requiredItems.Length);
			m_itemRequestComponent.SetRequiredItems(m_inventoryComponent.Inventory, m_requiredItems);
		}

		protected override void Start()
		{
			base.Start();
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

				// NOTE: We already have the BlueprintData reference! This is redundant work!

				// Create the final structure
				BlueprintData blueprintData = IndexRegistry.
					GetAsset<BlueprintData>(m_blueprintToSpawnOnCompletion.ID);

				// NOTE: Further talk on IndexRegistry:
				// I expect there are many similar cases like this in the project, I am side-eying
				// the IndexRegistry and suspicious that it may not actually serve a legitimate purpose.
				// My reasoning is that the only times you need to spawn an object without a reference are:
				// 1. Networked multiplayer
				// 2. Save system
				// We're not doing multiplayer, and the eventual save system should be worked on near the end (and would be structured differently anyways).
				// I believe that using the IndexRegistry will lead to many issues down the road like the SBTD one
				// did (merging issues, bugs, forgetting to generate ids or add objects, null refs), and cutting it when
				// that starts is the best course of action.

				GameObject prefab = blueprintData.PrefabSpawnedOnCompletion;
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

		public override void StopInteractSpeed() { }
	}
}
