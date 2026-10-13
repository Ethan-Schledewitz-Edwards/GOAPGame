using BehaviourTrees;
using Interaction.Core;
using InventorySystem;
using InventorySystem.Items;
using ObjectTags;
using Settlements;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Interaction.InteractableStructures
{
	[RequireComponent(typeof(InventoryComponent))]
	public class ItemStorageActorInteractable : ActorInteractableBase, IStructure, IItemFiltered
	{
		private static BehaviourTree s_takeItemBT;

		protected override int m_minActorsToFunction => 0;

		[Header("Settings")]
		public ItemTag[] ItemTagFilter => m_tagFilter;
		[SerializeField] private ItemTag[] m_tagFilter;
		[SerializeField] private StructureTag m_structureTypeTag;

		public InventoryComponent InventoryComponent { get; private set; }

		// IInteractable properties
		protected override string m_interactPrompt => string.Empty;
		protected override bool m_hasTrackingDisplayInfo => false;

		// IStructure properties
		private int m_settlementID;
		private int m_settlementStructureID;

		public StructureTag StructureTypeTag => m_structureTypeTag;
		public int SettlementID => m_settlementID;
		public int SettlementStructureID => m_settlementStructureID;
		public GameObject Object => gameObject;

		protected override void Awake()
		{
			base.Awake();

			InventoryComponent = GetComponent<InventoryComponent>();

			InitializeBehaviourTree();
		}

		private void InitializeBehaviourTree()
		{
			if (s_takeItemBT != null)
				return;

			BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
			{
				new BTTimeoutNode(new FindItemOfTagTask(), 2f),
				new BTTimeoutNode(new ReserveInteractionPositionTask(), 2f),
				new MoveToInteractionPointTask(),
				new InteractWithTargetTask(),
				new ReturnToStructureTask(),
				new MoveToInteractionPointTask(),
				new BTTimeoutNode(new DepositHeldItemTask(), 60f),
				new BTTimeoutNode(new AquireNewBehaviourTreeFromTargetTask(), 2f)
			});

			BehaviourTree tree = new BehaviourTree();
			tree.SetTree(root);
			s_takeItemBT = tree;
		}

		public void SetSettlement(int settlementID, int settlementStructureID)
		{
			m_settlementID = settlementID;
			m_settlementStructureID = settlementStructureID;
		}

		public override bool TryInteract(IInteractor interactor,
			Vector3 actorPosition,
			InteractionPoint reservedPosition)
		{
			if (!base.TryInteract(interactor, actorPosition, reservedPosition))
				return false;

			BehaviourTreeExecutorBase executor = interactor.Transform.GetComponent<BehaviourTreeExecutorBase>();
			if (executor != null)
			{
				if (m_tagFilter != null && m_tagFilter.Length > 0)
				{
					executor.AIContext.ItemTagFilters = m_tagFilter
						.Where(tag => tag != null)
						.Select(tag => tag.ID)
						.ToArray();
				}
				else
				{
					executor.AIContext.ItemTagFilters = null;
				}

				executor.AIContext.StructureSettlementID = m_settlementID;
				executor.AIContext.StructureID = m_settlementStructureID;
				return true;
			}

			base.StopInteract(interactor, reservedPosition);
			return false;
		}

		public override BehaviourTree GetBehaviourTree() => s_takeItemBT;
	}
}