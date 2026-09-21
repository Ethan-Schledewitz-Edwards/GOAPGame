using BehaviourTrees;
using Construction;
using Entities.Core;
using GenericIndex;
using InventorySystem;
using ObjectTags;
using Settlements;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Interaction.InteractableStructures.Blueprints
{
	[RequireComponent(typeof(InventoryComponent), typeof(BlueprintCancelation), typeof(ItemRequestComponent))]
	public abstract class BlueprintIO : InteractableObjectBase, IStructure, IItemFiltered
	{
		private static BehaviourTree s_cachedBlueprintBT;

		[Header("Structure Settings")]
		[SerializeField] private StructureTag m_structureTypeTag;
		[SerializeField] private int m_maxCapacity = 4;

		private Entity m_entity;
		protected BlueprintCancelation m_cancelBlueprint;
		protected ItemRequestComponent m_itemRequestComponent;
		protected InventoryComponent m_inventoryComponent;

		protected int m_settlementID;
		protected int m_settlementStructureID;

		public StructureTag StructureTypeTag => m_structureTypeTag;
		public int SettlementID => m_settlementID;
		public int SettlementStructureID => m_settlementStructureID;
		public GameObject Object => gameObject;

		[SerializeField] protected ItemTag[] m_tagFilter;
		public ItemTag[] ItemTagFilter => m_tagFilter;

		protected virtual void Awake()
		{
			InitializeBehaviourTree();

			m_entity = GetComponent<Entity>();
			m_entity.EnableDynamicPositionUpdates(false);

			m_cancelBlueprint = GetComponent<BlueprintCancelation>();
			m_inventoryComponent = GetComponent<InventoryComponent>();

			m_itemRequestComponent = GetComponent<ItemRequestComponent>();
			m_itemRequestComponent.ItemsAchieved += HandleBlueprintCompleted;
		}

		private void Start()
		{
			if (m_interactPositions == null || m_interactPositions.Length == 0)
				m_interactPositions = GetComponentsInChildren<InteractionPosition>();
		}

		protected virtual void OnDestroy()
		{
			if (m_itemRequestComponent != null)
				m_itemRequestComponent.ItemsAchieved -= HandleBlueprintCompleted;
		}

		private void InitializeBehaviourTree()
		{
			if (s_cachedBlueprintBT != null)
				return;

			StructureTag storageTag =
				IndexRegistry.GetAsset<StructureTag>("Storage_StructureTag");

			BTNodeBase findUseTask = new FindItemEntityOfIDTask(storageTag);
			BTTimeoutNode timeoutFind = new BTTimeoutNode(findUseTask, 2f);

			BTNodeBase reserveItemPositionTask = new ReserveInteractionPositionTask();

			BTNodeBase checkDestination1 = new CheckForDestinationRangeTask();
			BTTimeoutNode timeoutCheckDestination1 = new BTTimeoutNode(checkDestination1, 2f);

			BTNodeBase checkDestination2 = new CheckForDestinationRangeTask();
			BTTimeoutNode timeoutCheckDestination2 = new BTTimeoutNode(checkDestination2, 2f);

			BTNodeBase pickupTask = new TryPickupItemTask();
			BTTimeoutNode timeoutPickup = new BTTimeoutNode(pickupTask, 2f);

			BTNodeBase depositTask = new DepositHeldItemTask();
			BTTimeoutNode timeoutDeposit = new BTTimeoutNode(depositTask, 2f);

			BTNodeBase jobTask = new AquireNewBehaviourFromTargetTask();
			BTTimeoutNode timeoutJobSearch = new BTTimeoutNode(jobTask, 2f);

			BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
			{
				timeoutFind,
				reserveItemPositionTask,
				new MoveToInteractionPositionTask(),
				timeoutCheckDestination1,
				timeoutPickup,
				new ReturnToStructureTask(),
				new MoveToInteractionPositionTask(),
				timeoutCheckDestination2,
				timeoutDeposit,
				timeoutJobSearch
			});

			BehaviourTree tree = new BehaviourTree();
			tree.SetTree(root);
			s_cachedBlueprintBT = tree;
		}

		public override bool TryInteract(
			IInteractor interactor,
			Vector3 actorPosition,
			InteractionPosition reservedPosition,
			out int interactorValue)
		{
			// Get the behaviour tree through base interaction
			if (!base.TryInteract(interactor, actorPosition, reservedPosition, out interactorValue))
				return false;

			if (m_itemRequestComponent == null)
			{
				base.StopInteract(interactor, reservedPosition);
				return false;
			}

			// Tell the actor which items to find (Its behaviour tree will use the context to find the item)
			BehaviourTreeExecutorBase executor = interactor.Transform.GetComponent<BehaviourTreeExecutorBase>();
			if (executor != null && executor.AIContext != null)
			{
				if (interactor.Transform.TryGetComponent(out InventoryComponent inventoryComponent) &&
					inventoryComponent.Slots.Count > 0)
				{
					int requestedItemID =
						m_itemRequestComponent.RequestItem(inventoryComponent.Slots[0]);

					if (requestedItemID > -1)
					{
						executor.AIContext.SetData<int>(AIContextKeys.c_StructureSettlementID, m_settlementID);
						executor.AIContext.SetData<int>(AIContextKeys.c_StructureID, m_settlementStructureID);
						executor.AIContext.SetData<int>(AIContextKeys.c_ItemToFindID, requestedItemID);
						return true;
					}
				}
			}

			base.StopInteract(interactor, reservedPosition);
			return false;
		}

		public void SetSettlement(int settlementID, int settlementStructureID)
		{
			m_settlementID = settlementID;
			m_settlementStructureID = settlementStructureID;
		}

		public override void UpdateSpeed(int extra) { }

		public abstract void HandleBlueprintCompleted();

		public override BehaviourTree GetBehaviourTree() => s_cachedBlueprintBT;
	}
}
