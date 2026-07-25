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
	public class ItemStorageIO : InteractableObjectBase, IStructure<ItemStorageIO>
	{
		private static BehaviourTree s_cachedBT;

		// Components
		public InventoryComponent InventoryComponent { get; private set; }

		// System
		public StructureTag StructureTypeTag => m_structureTypeTag;
		[SerializeField] private StructureTag m_structureTypeTag;

		public int SettlementID { get => m_structureID; set => m_structureID = value; }
		private int m_settlementID;

		public int SettlementStructureID { get => m_structureID; set => m_structureID = value; }
		private int m_structureID;

		public GameObject StructureObject => gameObject;

		public int MaxCapacity => m_maxCapacity;
		[SerializeField] private int m_maxCapacity = 4;

		public int ActorsAssigned => m_actorsAssigned;
		[SerializeField] private int m_actorsAssigned = 0;

		[Header("Storage Configuration")]
		[SerializeField] private ItemTag[] m_permittedItemTypes;

		public override bool UseFormationRadius { get => false; }

		private void Awake()
		{
			InventoryComponent = GetComponent<InventoryComponent>();

			if (s_cachedBT == null)
			{
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

		public override void UpdateSpeed(int extra)
		{

		}

		public void AssignActor(out ItemStorageIO structure)
		{
			structure = null; // Storage should not have anyone assigned to it
		}

		public override bool TryInteract(IInteractor interactor, bool interactionTakesPriority)
		{
			BehaviourTreeExecutorBase executor = interactor.Transform.GetComponent<BehaviourTreeExecutorBase>();
			if (executor != null && executor.AIContext != null)
			{
				foreach (ItemTag tag in m_permittedItemTypes)
				{
					executor.AIContext.SetData<int>(AIContextKeys.c_ItemTagPrefix + tag.TagID, tag.TagID);
				}

				interactor.OnInteractWithObject(this, interactionTakesPriority);

				executor.AIContext.SetData<int>(AIContextKeys.c_HomeSettlementID, m_settlementID);
				executor.AIContext.SetData<int>(AIContextKeys.c_StructureID, SettlementStructureID);
			}

			// Construction workers
			AssignActor();
			return true;
		}

		public override BehaviourTree GetBehaviourTree() => s_cachedBT;
	}
}