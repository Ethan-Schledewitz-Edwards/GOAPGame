using Construction;
using AssetIndex.Core;
using InventorySystem;
using ObjectTags;
using Settlements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Interaction.Core;

namespace Interaction.ActorInteractables.Structures
{
	[RequireComponent(typeof(BoxCollider), typeof(BlueprintCancelation))]
	public class PlacedBlueprint : BlueprintActorInteractableBase, IBlueprintObject
	{
		protected override int m_minActorsToFunction => 0;

		[Header("Settings & Visuals")]
		[SerializeField] private Material m_blueprintMaterial;
		[SerializeField] private MeshFilter m_meshFilter;
		[SerializeField] private MeshRenderer m_meshRenderer;

		[Header("Components")]
		private BlueprintCancelation m_cancelBlueprint;
		private BoxCollider m_boxCollider;

		// IBlueprintObject properties
		public string m_blueprintDataID;

		public event Action<IBlueprintObject> BlueprintCompleted;
		public event Action<IBlueprintObject> BlueprintCanceled;

		protected override void Awake()
		{
			base.Awake();

			m_cancelBlueprint = GetComponent<BlueprintCancelation>();
			m_boxCollider = GetComponent<BoxCollider>();

			m_cancelBlueprint.CanceledBlueprint += HandleBlueprintCanceled;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			m_cancelBlueprint.CanceledBlueprint -= HandleBlueprintCanceled;
		}

		public void HandleBlueprintPlaced(BlueprintData blueprintData, Vector3 position, Quaternion rotation)
		{
			// Extract item tags from required items
			HashSet<ItemTag> uniqueTags = new HashSet<ItemTag>();
			if (blueprintData.RequiredItems != null)
			{
				foreach (var requiredItem in blueprintData.RequiredItems)
				{
					if (requiredItem.itemType is ITaggable<ItemTag> taggableItem && taggableItem.RuntimeTagSet != null)
					{
						uniqueTags.UnionWith(taggableItem.RuntimeTagSet);
					}
				}
			}

			m_blueprintDataID = blueprintData.ID;
			m_tagFilter = uniqueTags.ToArray();

			m_inventoryComponent.InitializeInventory(blueprintData.RequiredItems.Length);
			m_itemRequestComponent.SetRequiredItems(m_inventoryComponent.Inventory, blueprintData.RequiredItems);

			transform.position = position;
			transform.rotation = rotation;

			Debug.Log($"Starting blueprint: {blueprintData.DisplayName}");
		}

		public override void HandleBlueprintCompleted()
		{
			Debug.Log($"A blueprint of SettlementBlueprintID:{m_settlementStructureID} " +
				$"was completed in settlement:{SettlementID}.");

			// Create the final structure
			BlueprintData blueprintData = IndexRegistry.GetAsset<BlueprintData>(m_blueprintDataID);
			GameObject prefab = blueprintData.PrefabSpawnedOnCompletion;
			GameObject spawnedStructureObj = Instantiate(prefab, transform.position, transform.rotation);

			if (spawnedStructureObj.TryGetComponent(out IStructure builtStructure))
			{
				SettlementManager.s_WorldSettlements[SettlementID].AddStructure(builtStructure);
			}
			else
			{
				Debug.LogError($"Prefab {prefab.name} is missing an IStructure component!");
			}

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

		public override void LostMinimumActors() { }
	}
}
