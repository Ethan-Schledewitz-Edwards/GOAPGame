using Construction;
using Entities.Savable;
using GenericIndex;
using InventorySystem;
using InventorySystem.Items;
using Settlements;
using System;
using UnityEngine;

namespace Interaction.InteractableStructures.Blueprints
{
    public class EnvironmentalBlueprint : BlueprintIO, IBlueprintObject
	{
		[Header("Settings")]
		[SerializeField] private ItemQuantity[] m_requiredItems;

		[SerializeField] private BlueprintData m_blueprintToSpawnOnCompletion;
		[SerializeField] private GameObject m_objectEnabledOnCompletion;

		public event Action<IBlueprintObject> BlueprintCompleted;
		public event Action<IBlueprintObject> BlueprintCanceled;

		protected override void Awake()
		{
			base.Awake();

			m_itemRequestComponent.SetRequiredItems(m_inventoryComponent.Inventory, m_requiredItems);

			m_saveableEntity.InitializeSavableEntity();
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
					Debug.LogError($"Prefab {prefab.name} is missing an IStructure component!");
				}

				if (spawnedStructureObj.TryGetComponent(out SaveableEntity saveableEntity))
				{
					saveableEntity.InitializeSavableEntity();
				}
			}

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
