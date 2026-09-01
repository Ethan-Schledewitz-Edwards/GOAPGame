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
	[RequireComponent(typeof(InventoryComponent), typeof(BlueprintCancelation), typeof(SaveableEntity))]
	public abstract class BlueprintIO : InteractableObjectBase, IStructure<BlueprintIO>, IItemFiltered
	{
		private static BehaviourTree s_cachedBlueprintBT;

		[Header("Structure Settings")]
		[SerializeField] private StructureTag m_structureTypeTag;
		[SerializeField] private int m_maxCapacity = 4;
		[SerializeField] private int m_actorsAssigned = 0;

		// Components
		private Entity m_entity;
		protected BlueprintCancelation m_cancelBlueprint;
		protected ItemRequestComponent m_itemRequestComponent;
		protected InventoryComponent m_inventoryComponent;
		protected SaveableEntity m_saveableEntity;

		// System
		protected int m_settlementID;
		protected int m_settlementStructureID;

		// IStructure Properties
		public StructureTag StructureTypeTag => m_structureTypeTag;
		public int SettlementID => m_settlementID;
		public int SettlementStructureID => m_settlementStructureID;
		public GameObject Object => gameObject;
		public int MaxCapacity => m_maxCapacity;
		public int ActorsAssigned => m_actorsAssigned;

		// IItemFiltered Properties
		[SerializeField] protected ItemTag[] m_tagFilter;
		public ItemTag[] ItemTagFilter => m_tagFilter;

		// Base
		public override bool UseFormationRadius => false;

		protected virtual void Awake()
		{
			InitializeBehaviourTree();

			m_entity = GetComponent<Entity>();
			m_entity.EnableDynamicPositionUpdates(false);

			m_cancelBlueprint = GetComponent<BlueprintCancelation>();

			m_inventoryComponent = GetComponent<InventoryComponent>();
			m_saveableEntity = GetComponent<SaveableEntity>();

			m_itemRequestComponent = GetComponent<ItemRequestComponent>();
			if (m_itemRequestComponent == null)
			{
				m_itemRequestComponent = gameObject.AddComponent<ItemRequestComponent>();
			}

			m_itemRequestComponent.ItemsAchieved += HandleBlueprintCompleted;
		}

		protected virtual void OnDestroy()
		{
			if (m_itemRequestComponent != null)
			{
				m_itemRequestComponent.ItemsAchieved -= HandleBlueprintCompleted;
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
					executor.AIContext.SetData<int>(AIContextKeys.c_StructureSettlementID, m_settlementID);
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
				new CheckForDestinationRangeTask(),
				new TryPickupItemTask(),
				new ReturnToStructureTask(),
				new MoveToTargetDataTask(),
				new CheckForDestinationRangeTask(),
				new DepositHeldItemTask(),
				timeoutJobSearch // Try to loop item search
			});
			tree.SetTree(root);
			s_cachedBlueprintBT = tree;
		}

		public void HandleAddedToSettlement(int settlementID, int settlementStructureID)
		{
			m_settlementID = settlementID;
			m_settlementStructureID = settlementStructureID;
		}

		/// <summary>
		/// Concrete implementations define what happens when all of the blueprints required items are gathered.
		/// </summary>
		public abstract void HandleBlueprintCompleted();
	}
}
