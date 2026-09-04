using Construction;
using GenericIndex;
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
    public class EnvironmentalBlueprint : BlueprintIO, IBlueprintObject
	{
		[Header("Settings")]
		[SerializeField] private ItemQuantity[] m_requiredItems;
		[SerializeField] private int m_blueprintSettlementID;

		private void OnValidate()
		{
			m_settlementID = m_blueprintSettlementID;
		}

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
					if (requiredItem.itemType is ITaggable<ItemTag> taggableItem && taggableItem.RuntimeTagSet != null)
					{
						uniqueTags.UnionWith(taggableItem.RuntimeTagSet);
					}
				}
			}

			m_tagFilter = uniqueTags.ToArray();

			m_inventoryComponent.InitializeInventory(m_requiredItems.Length);
			m_itemRequestComponent.SetRequiredItems(m_inventoryComponent.Inventory, m_requiredItems);
		}

		private void Start()
		{
			if(SettlementManager.s_WorldSettlements.TryGetValue(m_settlementID, out Settlement settlement))
			{
				settlement.AddStructure(this);
			}
			else
			{
				Debug.LogWarning("Tried to add an EnvironmentalBlueprint to a settlement that does not exist", this);
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
				Debug.Log($"A blueprint of SettlementBlueprintID:{m_settlementStructureID} was completed in settlement:{SettlementID}.");

				// Create the final structure
				BlueprintData blueprintData = IndexRegistry.GetAsset<BlueprintData>(m_blueprintToSpawnOnCompletion.BlueprintDataID);
				GameObject prefab = blueprintData.BlueprintFeatureData.Prefab;
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

			Debug.Log($"A blueprint of SettlementBlueprintID:{m_settlementStructureID} was completed in settlement:{SettlementID}.");
			BlueprintCompleted?.Invoke(this);
		}

		public void HandleBlueprintCanceled()
		{
			Debug.Log($"A blueprint of SettlementBlueprintID:{m_settlementStructureID} was canceled in settlement:{SettlementID}.");

			foreach (InventorySlot slot in m_inventoryComponent.Slots)
			{
				slot.RemoveFromStack(slot.AmountInSlot, out var _, true, transform.position);
			}

			BlueprintCanceled?.Invoke(this);
		}

		public void HandleBlueprintPlaced(BlueprintData structureBlueprintData, Vector3 position, Quaternion rotation) { }
	}
}
