using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;
using InventorySystem;
using InventorySystem.Items;
using Settlements;

namespace Interaction.InteractableStructures
{
	[RequireComponent(typeof(InventoryComponent))]
	public class ItemStorageIO : InteractableObjectBase, IStructure<ItemStorageIO>
	{
		private static BehaviourTree m_ItemStorageBT;

		// Components
		public InventoryComponent InventoryComponent { get; private set; }

		// System
		public string StructureTypeKey => "Storage";
		public int StructureID { get => m_structureID; set => m_structureID = value; }
		private int m_structureID;

		public GameObject StructureObject => gameObject;

		public int MaxCapacity => m_maxCapacity;
		[SerializeField] private int m_maxCapacity = 4;

		public int ActorsAssigned => m_actorsAssigned;
		[SerializeField] private int m_actorsAssigned = 0;

		[Header("Storage Configuration")]
		[SerializeField] private ItemData m_itemType;
		public ItemData ItemType => m_itemType;

		public override bool UseFormationRadius { get => false; }


		private void Awake()
		{
			InventoryComponent = GetComponent<InventoryComponent>();

			if (m_ItemStorageBT == null)
			{
				BehaviourTree tree = new BehaviourTree();
				BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
				{

				});
				tree.SetTree(root);
				m_ItemStorageBT = tree;
			}
		}

		public override void UpdateSpeed(int extra)
		{

		}

		public void AssignActor(out ItemStorageIO structure)
		{
			structure = null; // Storage should not have anyone assigned to it
		}

		public override BehaviourTree GetBehaviourTree() => m_ItemStorageBT;
	}
}