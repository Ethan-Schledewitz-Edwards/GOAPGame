using Construction;
using Entities.Savable;
using InventorySystem;
using ObjectTags;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Interaction.InteractableStructures.Blueprints
{
	[RequireComponent(typeof(BoxCollider), typeof(BlueprintCancelation))]
	public class PlacedBlueprint : BlueprintIO, IBlueprintObject
	{
		[Header("Settings & Visuals")]
		[SerializeField] private Material m_blueprintMaterial;
		[SerializeField] private MeshFilter m_meshFilter;
		[SerializeField] private MeshRenderer m_meshRenderer;

		public override event Action<IBlueprintObject> BlueprintCompleted;
		public override event Action<IBlueprintObject> BlueprintCanceled;

		[Header("Components")]
		private BoxCollider m_boxCollider;
		private BlueprintCancelation m_cancelBlueprint;

		// IBlueprintObject Properties
		public int BlueprintDataID => m_blueprintDataID;

		protected override void Awake()
		{
			base.Awake();

			m_boxCollider = GetComponent<BoxCollider>();

			m_cancelBlueprint = GetComponent<BlueprintCancelation>();
			m_cancelBlueprint.CanceledBlueprint += HandleBlueprintCanceled;
		}

		protected override void OnDestroy()
		{
			if (m_cancelBlueprint != null)
			{
				m_cancelBlueprint.CanceledBlueprint -= HandleBlueprintCanceled;
			}

			base.OnDestroy();
		}

		public void HandleBlueprintStarted(BlueprintData blueprintData, Vector3 position, Quaternion rotation)
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

			m_blueprintDataID = blueprintData.BlueprintDataID;
			m_tagFilter = uniqueTags.ToArray();

			m_inventoryComponent.InitializeInventory(blueprintData.RequiredItems.Length);
			m_itemRequestComponent.SetRequiredItems(m_inventoryComponent.Inventory, blueprintData.RequiredItems);

			SetBlueprintMesh(blueprintData.BlueprintMesh);
			SetInteractionOffsetTransform(m_interactOffset, blueprintData.InteractionLocalOffset);

			transform.position = position;
			transform.rotation = rotation;

			Debug.Log($"Starting blueprint: {blueprintData.DisplayName}");
		}

		public override void HandleBlueprintCanceled()
		{
			Debug.Log($"A blueprint of SettlementBlueprintID:{m_settlementStructureID} was canceld in settlement:{SettlementID}.");
			BlueprintCanceled?.Invoke(this);

			foreach (InventorySlot slot in m_inventoryComponent.Slots)
			{
				slot.RemoveFromStack(slot.AmountInSlot, out var _, true, transform.position);
			}

			Destroy(gameObject);
		}

		public override void HandleBlueprintCompleted()
		{
			m_saveableEntity.InitializeSavableEntity();

			Debug.Log($"A blueprint of SettlementBlueprintID:{m_settlementStructureID} was completed in settlement:{SettlementID}.");
			BlueprintCompleted?.Invoke(this);
		}

		private void SetBlueprintMesh(Mesh blueprintMesh)
		{
			m_meshFilter.mesh = blueprintMesh;
			m_meshRenderer.material = m_blueprintMaterial;

			// Adjust the blueprint's box collider to match the mesh
			Bounds meshBounds = blueprintMesh.bounds;
			m_boxCollider.size = meshBounds.size;
			m_boxCollider.center = Vector3.up * meshBounds.extents.y;
		}
	}
}
