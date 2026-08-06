using BehaviourTrees;
using Construction;
using Entities.Core;
using Entities.Savable;
using GenericIndex;
using InventorySystem;
using ObjectTags;
using Settlements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Interaction.InteractableStructures.Blueprints
{
	[RequireComponent(typeof(BoxCollider), 
		typeof(InventoryComponent), 
		typeof(BlueprintCancelation))]
	public class BlueprintIO : InteractableObjectBase, IStructure<BlueprintIO>, IBlueprintObject, IItemFiltered
	{
		private static BehaviourTree s_cachedBlueprintBT;

		[Header("Settings & Visuals")]
		[SerializeField] private Material m_blueprintMaterial;
		[SerializeField] private MeshFilter m_meshFilter;
		[SerializeField] private MeshRenderer m_meshRenderer;
		[SerializeField] private StructureTag m_structureTypeTag;
		[SerializeField] private int m_maxCapacity = 4;
		[SerializeField] private int m_actorsAssigned = 0;

		// Components
		private BoxCollider m_boxCollider;
		private BlueprintCancelation m_cancelBlueprint;
		private Entity m_entity;
		private ItemRequestComponent m_itemRequestComponent;
		private InventoryComponent m_inventoryComponent;
		private SaveableEntity m_saveableEntity;

		// Events
		public event Action<IBlueprintObject> BlueprintCompleted;
		public event Action<IBlueprintObject> BlueprintCanceled;

		// System
		private int m_settlementID;
		private int m_settlementStructureID;
		private int m_blueprintDataID;
		private ItemTag[] m_tagFilter;

		// IStructure Properties
		public StructureTag StructureTypeTag => m_structureTypeTag;
		public int SettlementID => m_settlementID;
		public int SettlementStructureID => m_settlementStructureID;
		public GameObject Object => gameObject;
		public int MaxCapacity => m_maxCapacity;
		public int ActorsAssigned => m_actorsAssigned;

		// IBlueprintObject Properties
		public int BlueprintDataID => m_blueprintDataID;

		// IItemFiltered Properties
		public ItemTag[] ItemTagFilter => m_tagFilter;

		// Base
		public override bool UseFormationRadius => false;

		private void Awake()
		{
			InitializeBehaviourTree();

			m_entity = GetComponent<Entity>();
			m_entity.EnableDynamicPositionUpdates(false);

			m_boxCollider = GetComponent<BoxCollider>();
			m_inventoryComponent = GetComponent<InventoryComponent>();

			m_cancelBlueprint = GetComponent<BlueprintCancelation>();
			m_cancelBlueprint.CanceledBlueprint += HandleBlueprintCanceled;

			m_saveableEntity = GetComponent<SaveableEntity>();

			m_itemRequestComponent = GetComponent<ItemRequestComponent>();
			if (m_itemRequestComponent == null)
			{
				m_itemRequestComponent = gameObject.AddComponent<ItemRequestComponent>();
			}

			m_itemRequestComponent.ItemsAchieved += HandleBlueprintCompleted;
		}

		private void OnDestroy()
		{
			if (m_itemRequestComponent != null)
			{
				m_itemRequestComponent.ItemsAchieved -= HandleBlueprintCompleted;
			}

			if (m_cancelBlueprint != null)
			{
				m_cancelBlueprint.CanceledBlueprint -= HandleBlueprintCanceled;
			}

			m_saveableEntity.UnregisterFromCurrentChunk();
		}

		public override void UpdateSpeed(int extra) { }

		public override BehaviourTree GetBehaviourTree() => s_cachedBlueprintBT;

		public void AssignActor(out BlueprintIO structure)
		{
			if (ActorsAssigned < MaxCapacity)
			{
				m_actorsAssigned++;
			}

			structure = this;
		}

		public override bool TryInteract(IInteractor interactor, bool interactionTakesPriority)
		{
			if(m_itemRequestComponent == null)
				return false;

			BehaviourTreeExecutorBase executor = interactor.Transform.GetComponent<BehaviourTreeExecutorBase>();
			if (executor != null && executor.AIContext != null)
			{
				interactor.Transform.TryGetComponent(out InventoryComponent inventoryComponent);

				int requestedItemID = m_itemRequestComponent.RequestItem(inventoryComponent.Slots[0]);
				if (requestedItemID > -1)
				{
					executor.AIContext.SetData<int>(AIContextKeys.c_HomeSettlementID, m_settlementID);
					executor.AIContext.SetData<int>(AIContextKeys.c_StructureID, m_settlementStructureID);

					interactor.OnInteractWithObject(this, interactionTakesPriority);
					executor.AIContext.SetData<int>(AIContextKeys.c_ItemToFindID, requestedItemID);

					return true;
				}
			}

			// Construction workers
			AssignActor(out _);
			return true;
		}

		public void AddStructureToSettlement(int settlementID, int settlementStructureID)
		{
			m_settlementID = settlementID;
			m_settlementStructureID = settlementStructureID;
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

		public void HandleBlueprintCompleted()
		{
			m_saveableEntity.InitializeSavableEntity();

			Debug.Log($"A blueprint of SettlementBlueprintID:{m_settlementStructureID} was completed in settlement:{SettlementID}.");
			BlueprintCompleted?.Invoke(this);
		}

		public void HandleBlueprintCanceled()
		{
			Debug.Log($"A blueprint of SettlementBlueprintID:{m_settlementStructureID} was canceld in settlement:{SettlementID}.");
			BlueprintCanceled?.Invoke(this);

			foreach (InventorySlot slot in m_inventoryComponent.Slots)
			{
				slot.RemoveFromStack(slot.AmountInSlot, out var _, true, transform.position);
			}

			Destroy(gameObject);
		}

		private void InitializeBehaviourTree()
		{
			if (s_cachedBlueprintBT != null)
				return;

			StructureTag storageTag = IndexRegistry.GetAsset<StructureTag>("Storage_StructureTag");

			BTNodeBase findUseTask = new FindItemEntityOfIDTask(storageTag);
			BTTimeoutNode timeoutFind = new BTTimeoutNode(findUseTask, 2f);

			BTNodeBase jobTask = new AquireJobFromTargetTask();
			BTTimeoutNode timeoutJobSearch = new BTTimeoutNode(jobTask, 2f);

			BehaviourTree tree = new BehaviourTree();
			BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
			{
				timeoutFind,
				new MoveToTargetDataTask(),
				new CheckForTargetRangeTask(),
				new TryGetItemTask(),
				new ReturnToStructureTask(),
				new MoveToTargetDataTask(),
				new CheckForTargetRangeTask(),
				new DepositHeldItemTask(),
				timeoutJobSearch // Try to loop item search
			});
			tree.SetTree(root);
			s_cachedBlueprintBT = tree;
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
