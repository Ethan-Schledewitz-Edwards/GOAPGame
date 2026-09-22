using BehaviourTrees;
using Entities.Core;
using InventorySystem;
using InventorySystem.Items;
using ObjectTags;
using Settlements;
using System.Collections.Generic;
using UnityEngine;

namespace Interaction.InteractableStructures
{
	[RequireComponent(typeof(InventoryComponent), typeof(Entity))]
	public class ItemStorageIO : InteractableObjectBase, IStructure, IItemFiltered
	{
		private static BehaviourTree s_takeItemBT;

		[Header("Settings")]
		public ItemTag[] ItemTagFilter => m_tagFilter;
		[SerializeField] private ItemTag[] m_tagFilter;
		[SerializeField] private StructureTag m_structureTypeTag;
		[SerializeField] private int m_maxCapacity = 4;
		[SerializeField] private int m_actorsAssigned = 0;

		private Entity m_entity;
		public InventoryComponent InventoryComponent { get; private set; }

		private int m_settlementID;
		private int m_settlementStructureID;

		public StructureTag StructureTypeTag => m_structureTypeTag;
		public int SettlementID => m_settlementID;
		public int SettlementStructureID => m_settlementStructureID;
		public GameObject Object => gameObject;

		private void Awake()
		{
			m_entity = GetComponent<Entity>();
			m_entity.EnableDynamicPositionUpdates(false);
			InventoryComponent = GetComponent<InventoryComponent>();

			InitializeBehaviourTree();
		}

		private void Start()
		{
			if (m_interactPositions == null || m_interactPositions.Length == 0)
				m_interactPositions = GetComponentsInChildren<InteractionPosition>();
		}

		private void InitializeBehaviourTree()
		{
			if (s_takeItemBT != null)
				return;

			BTNodeBase findItemTimeoutSequence = new BTTimeoutNode(new BTSequenceNode(new List<BTNodeBase>
			{
				new FindItemEntityOfTagTask(),
				new ReserveInteractionPositionTask()
			}),
			2.0f);

			BTNodeBase depositTask = new DepositHeldItemTask();
			BTTimeoutNode timeoutDeposit = new BTTimeoutNode(depositTask, 60f);

			BTNodeBase jobTask = new AquireNewBehaviourTreeFromTargetTask();
			BTTimeoutNode findItemTimeout = new BTTimeoutNode(jobTask, 2f);

			BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
			{
				findItemTimeoutSequence,
				new MoveToInteractionPositionTask(),
				new CheckForDestinationRangeTask(),
				new InteractWithTargetTask(),
				new ReturnToStructureTask(),
				new MoveToInteractionPositionTask(),
				new CheckForDestinationRangeTask(),
				timeoutDeposit,
				findItemTimeout
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

		public override bool TryInteract(
			IInteractor interactor,
			Vector3 actorPosition,
			InteractionPosition reservedPosition,
			out int interactorValue)
		{
			if (!base.TryInteract(interactor, actorPosition, reservedPosition, out interactorValue))
				return false;

			BehaviourTreeExecutorBase executor = interactor.Transform.GetComponent<BehaviourTreeExecutorBase>();
			if (executor != null && executor.AIContext != null)
			{
				foreach (ItemTag tag in m_tagFilter)
				{
					executor.AIContext.SetData<int>(AIContextKeys.c_ItemTagPrefix + tag.TagID, tag.TagID);
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

		public override void UpdateSpeed(int extra) { }

		public override void StopInteractSpeed() { }

		public override BehaviourTree GetBehaviourTree() => s_takeItemBT;
	}
}