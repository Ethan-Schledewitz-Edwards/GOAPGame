using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;
using InventorySystem;
using InventorySystem.Items;
using Settlements;
using ObjectTags;

namespace Interaction.InteractableStructures
{
	[RequireComponent(typeof(InventoryComponent))]
	public class ItemStorageIO : InteractableObjectBase, IStructure<ItemStorageIO>, IItemFiltered
	{
		private static BehaviourTree s_cachedBT;

		[Header("Settings")]
		public ItemTag[] ItemTagFilter => m_tagFilter;
		[SerializeField] private ItemTag[] m_tagFilter;
		[SerializeField] private StructureTag m_structureTypeTag;
		[SerializeField] private int m_maxCapacity = 4;
		[SerializeField] private int m_actorsAssigned = 0;

		// Components
		public InventoryComponent InventoryComponent { get; private set; }

		// System
		private int m_settlementID;
		private int m_settlementStructureID;

		// IStructure Properties
		public StructureTag StructureTypeTag => m_structureTypeTag;
		public int SettlementID => m_settlementID;
		public int SettlementStructureID => m_settlementStructureID;
		public GameObject Object => gameObject;
		public int MaxCapacity => m_maxCapacity;
		public int ActorsAssigned => m_actorsAssigned;

		public override bool UseFormationRadius { get => false; }

		private void Awake()
		{
			InventoryComponent = GetComponent<InventoryComponent>();

			InitializeBehaviourTree();
		}

		public void AddStructureToSettlement(int settlementID, int settlementStructureID)
		{
			m_settlementID = settlementID;
			m_settlementStructureID = settlementStructureID;
		}

		public override void UpdateSpeed(int extra) { }

		public void AssignActor(out ItemStorageIO structure)
		{
			structure = null; // Storage should not have anyone assigned to it
		}

		public override BehaviourTree GetBehaviourTree() => s_cachedBT;

		public override bool TryInteract(IInteractor interactor, bool interactionTakesPriority)
		{
			BehaviourTreeExecutorBase executor = interactor.Transform.GetComponent<BehaviourTreeExecutorBase>();
			if (executor != null && executor.AIContext != null)
			{
				foreach (ItemTag tag in m_tagFilter)
				{
					executor.AIContext.SetData<int>(AIContextKeys.c_ItemTagPrefix + tag.TagID, tag.TagID);
				}

				interactor.OnInteractWithObject(this, interactionTakesPriority);

				executor.AIContext.SetData<int>(AIContextKeys.c_HomeSettlementID, m_settlementID);
				executor.AIContext.SetData<int>(AIContextKeys.c_StructureID, SettlementStructureID);

				return true;
			}

			return false;
		}

		private void InitializeBehaviourTree()
		{
			if (s_cachedBT == null)
				return;

			BTNodeBase findUseTask = new FindItemEntityOfTagTask();
			BTTimeoutNode timeoutFind = new BTTimeoutNode(findUseTask, 2f);

			BTNodeBase jobTask = new AquireJobFromTargetTask();
			BTTimeoutNode timeoutJobSearch = new BTTimeoutNode(jobTask, 2f);

			BehaviourTree tree = new BehaviourTree();
			BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
				{
					timeoutFind,
					new MoveToTargetDataTask(),
					new CheckForTargetRangeTask(),
					new InteractWithTargetTask(),
					new ReturnToStructureTask(),
					new MoveToTargetDataTask(),
					new CheckForTargetRangeTask(),
					new DepositHeldItemTask(),
					timeoutJobSearch // Try to loop item search
				});
			tree.SetTree(root);
			s_cachedBT = tree;
		}
	}
}