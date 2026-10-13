using BehaviourTrees;
using Construction;
using AssetIndex.Core;
using InventorySystem;
using ObjectTags;
using Settlements;
using System;
using System.Collections.Generic;
using UnityEngine;
using Interaction.Core;

namespace Interaction.ActorInteractables.Structures
{
	[RequireComponent(typeof(InventoryComponent), typeof(ItemRequestComponent))]
	public abstract class BlueprintActorInteractableBase : ActorInteractableBase, IStructure, IItemFiltered
	{
		private static BehaviourTree s_cachedBlueprintBT;

		[Header("Structure Settings")]
		[SerializeField] private StructureTag m_structureTypeTag;

		protected ItemRequestComponent m_itemRequestComponent;
		protected InventoryComponent m_inventoryComponent;

		protected int m_settlementID;
		protected int m_settlementStructureID;

		// IInteractable properties
		protected override string m_interactPrompt => string.Empty;
		protected override bool m_hasTrackingDisplayInfo => true;

		// IStructure properties
		public StructureTag StructureTypeTag => m_structureTypeTag;
		public int SettlementID => m_settlementID;
		public int SettlementStructureID => m_settlementStructureID;
		public GameObject Object => gameObject;

		// IItemFiltered properties
		[SerializeField] protected ItemTag[] m_tagFilter;
		public ItemTag[] ItemTagFilter => m_tagFilter;

		protected virtual void Awake()
		{
			InitializeBehaviourTree();

			m_inventoryComponent = GetComponent<InventoryComponent>();
			m_itemRequestComponent = GetComponent<ItemRequestComponent>();
			m_itemRequestComponent.ItemsAchieved += HandleBlueprintCompleted;
		}

		protected virtual void Start()
		{
			if (m_interactPositions == null || m_interactPositions.Length == 0)
				m_interactPositions = GetComponentsInChildren<InteractionPoint>();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

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
				new BTTimeoutNode(new FindItemOfIDTask(storageTag), 2f),
				new ReserveInteractionPositionTask(),
				new MoveToInteractionPointTask(),
				new BTTimeoutNode(new TryPickupItemTask(), 2f),
				new ReturnToStructureTask(),
				new MoveToInteractionPointTask(),
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
			InteractionPoint reservedPosition)
		{
			// Get the behaviour tree through base interaction
			if (!base.TryInteract(interactor, actorPosition, reservedPosition))
				return false;

			if (m_itemRequestComponent == null)
			{
				base.StopInteract(interactor, reservedPosition);
				return false;
			}

			// Tell the actor which items to find (Its behaviour tree will use the context to find the item)
			BehaviourTreeExecutorBase executor = interactor.Transform.GetComponent<BehaviourTreeExecutorBase>();
			if (executor != null)
			{
				if (interactor.Transform.TryGetComponent(out InventoryComponent inventoryComponent) &&
					inventoryComponent.Slots.Count > 0)
				{
					string requestedItemID = m_itemRequestComponent.RequestItem(inventoryComponent.Slots[0]);

					if (string.IsNullOrEmpty(requestedItemID))
						return false;

					var context = executor.AIContext;
					context.StructureSettlementID = m_settlementID;
					context.StructureID = m_settlementStructureID;
					context.ItemToFindID = requestedItemID;

					executor.AIContext = context;
					return true;
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

		public abstract void HandleBlueprintCompleted();

		public override BehaviourTree GetBehaviourTree() => s_cachedBlueprintBT;
	}
}
