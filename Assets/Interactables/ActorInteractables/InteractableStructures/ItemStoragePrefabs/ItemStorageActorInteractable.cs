using BehaviourTrees;
using InventorySystem;
using InventorySystem.Items;
using ObjectTags;
using Settlements;
using System.Collections.Generic;
using UnityEngine;

namespace Interaction.InteractableStructures
{
	[RequireComponent(typeof(InventoryComponent))]
	public class ItemStorageActorInteractable : ActorInteractableBase, IStructure, IItemFiltered
	{
		private static BehaviourTree s_takeItemBT;

		protected override int m_minActorsToFunction { get; set; } = 0;

		[Header("Settings")]
		public ItemTag[] ItemTagFilter => m_tagFilter;
		[SerializeField] private ItemTag[] m_tagFilter;
		[SerializeField] private StructureTag m_structureTypeTag;
		[SerializeField] private int m_maxCapacity = 4;
		[SerializeField] private int m_actorsAssigned = 0;

		public InventoryComponent InventoryComponent { get; private set; }

		private int m_settlementID;
		private int m_settlementStructureID;

		public StructureTag StructureTypeTag => m_structureTypeTag;
		public int SettlementID => m_settlementID;
		public int SettlementStructureID => m_settlementStructureID;
		public GameObject Object => gameObject;

		private void Awake()
		{
			InventoryComponent = GetComponent<InventoryComponent>();

			InitializeBehaviourTree();
		}

		private void Start()
		{
			if (m_interactPositions == null || m_interactPositions.Length == 0)
				m_interactPositions = GetComponentsInChildren<InteractionPoint>();
		}

		private void InitializeBehaviourTree()
		{
			if (s_takeItemBT != null)
				return;

			BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
			{
				new BTTimeoutNode(new FindItemOfTagTask(), 2f),
				new BTTimeoutNode(new ReserveInteractionPositionTask(), 2f),
				new MoveToInteractionPositionTask(),
				new CheckForDestinationRangeTask(),
				new InteractWithTargetTask(),
				new ReturnToStructureTask(),
				new MoveToInteractionPositionTask(),
				new CheckForDestinationRangeTask(),
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
			InteractionPoint reservedPosition,
			out int interactorValue)
		{
			if (!base.TryInteract(interactor, actorPosition, reservedPosition, out interactorValue))
				return false;

			BehaviourTreeExecutorBase executor = interactor.Transform.GetComponent<BehaviourTreeExecutorBase>();
			if (executor != null && executor.AIContext != null)
			{
				foreach (ItemTag tag in m_tagFilter)
				{
					executor.AIContext.SetData<string>(AIContextKeys.c_ItemTagFilterPrefix + tag.ID, tag.ID);
				}

				executor.AIContext.SetData<int>(AIContextKeys.c_StructureSettlementID, m_settlementID);
				executor.AIContext.SetData<int>(AIContextKeys.c_StructureID, m_settlementStructureID);
				m_actorsAssigned = GetTotalActorsPresent();
				return true;
			}

			base.StopInteract(interactor, reservedPosition);

			interactorValue = -1;
			return false;
		}

		public override BehaviourTree GetBehaviourTree() => s_takeItemBT;
	}
}