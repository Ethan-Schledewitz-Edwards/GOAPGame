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
	[RequireComponent(typeof(InventoryComponent), typeof(ItemRequestComponent))]
	public abstract class BlueprintIOBase : InteractableObjectBase, IStructure, IItemFiltered
	{
		private static BehaviourTree s_cachedBlueprintBT;

		[Header("Structure Settings")]
		[SerializeField] private StructureTag m_structureTypeTag;

		private Entity m_entity;
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

			StructureTag storageTag = IndexRegistry.GetAsset<StructureTag>("Storage_StructureTag");

			BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
			{
				new BTTimeoutNode(new FindItemEntityOfIDTask(storageTag), 2f),
				new ReserveInteractionPositionTask(),
				new MoveToInteractionPositionTask(),
				new BTTimeoutNode(new CheckForDestinationRangeTask(), 2f),
				new BTTimeoutNode(new TryPickupItemTask(), 2f),
				new ReturnToStructureTask(),
				new MoveToInteractionPositionTask(),
				new BTTimeoutNode(new CheckForDestinationRangeTask(), 2f),
				new BTTimeoutNode(new DepositHeldItemTask(), 2f),
				new ReserveInteractionPositionTask(),
				new BTTimeoutNode(new AquireNewBehaviourTreeFromTargetTask(), 2f)
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
