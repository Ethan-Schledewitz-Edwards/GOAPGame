using BehaviourTrees;
using Entities.Core;
using Entities.Savable;
using InventorySystem;
using InventorySystem.Items;
using ObjectTags;
using Settlements;
using System.Collections.Generic;
using UnityEngine;

namespace Interaction.InteractableStructures
{
	[RequireComponent(typeof(InventoryComponent), typeof(Entity), typeof(SaveableEntity))]
	public class ItemStorageIO : InteractableObjectBase, IStructure<ItemStorageIO>, IItemFiltered
	{
		private static BehaviourTree s_takeItemBT;

		[Header("Settings")]
		public ItemTag[] ItemTagFilter => m_tagFilter;
		[SerializeField] private ItemTag[] m_tagFilter;
		[SerializeField] private StructureTag m_structureTypeTag;
		[SerializeField] private int m_maxCapacity = 4;
		[SerializeField] private int m_actorsAssigned = 0;

		// Components
		private Entity m_entity;
		public InventoryComponent InventoryComponent { get; private set; }
		private SaveableEntity m_saveableEntity;

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
			m_entity = GetComponent<Entity>();
			m_entity.EnableDynamicPositionUpdates(false);

			InventoryComponent = GetComponent<InventoryComponent>();
			m_saveableEntity = GetComponent<SaveableEntity>();

			InitializeBehaviourTree();
		}

		private void OnEnable()
		{
			m_saveableEntity.InitializeSavableEntity();
		}

		private void OnDisable()
		{
			m_saveableEntity.UnregisterFromCurrentChunk();
		}

		private void OnDestroy()
		{
			m_saveableEntity.UnregisterFromCurrentChunk();
		}

		public void HandleAddedToSettlement(int settlementID, int settlementStructureID)
		{
			m_settlementID = settlementID;
			m_settlementStructureID = settlementStructureID;
		}

		public override void UpdateSpeed(int extra) { }

		public void AssignActor(out ItemStorageIO structure)
		{
			structure = null; // Storage should not have anyone assigned to it
		}

		public override BehaviourTree GetBehaviourTree() => s_takeItemBT;

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

				executor.AIContext.SetData<int>(AIContextKeys.c_StructureSettlementID, m_settlementID);
				executor.AIContext.SetData<int>(AIContextKeys.c_StructureID, SettlementStructureID);

				return true;
			}

			return false;
		}

		private void InitializeBehaviourTree()
		{
			if (s_takeItemBT != null)
				return;

			// Create the find item task sequence
			BTNodeBase findUseTask = new FindItemEntityOfTagTask();
			BTTimeoutNode timeoutFind = new BTTimeoutNode(findUseTask, 2f);

			BTNodeBase depositTask = new DepositHeldItemTask();
			BTTimeoutNode timeoutDeposit = new BTTimeoutNode(depositTask, 2f);

			BTNodeBase jobTask = new AquireJobFromTargetTask();
			BTTimeoutNode timeoutJobSearch = new BTTimeoutNode(jobTask, 2f);

			BehaviourTree tree = new BehaviourTree();
			BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
				{
					timeoutFind,
					new MoveToTargetDataTask(),
					new CheckForDestinationRangeTask(),
					new InteractWithTargetTask(),
					new ReturnToStructureTask(),
					new MoveToTargetDataTask(),
					new CheckForDestinationRangeTask(),
					depositTask,
					timeoutJobSearch // Try to loop item search
				});
			tree.SetTree(root);
			s_takeItemBT = tree;
		}
	}
}