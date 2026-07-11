using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;
using Settlements;

namespace Interaction.InteractableStructures
{
	public class ActorHouseIO : InteractableObjectBase, IStructure<ActorHouseIO>
	{
		private static BehaviourTree m_cachedHousingBT;

		// System
		public string StructureTypeKey => "House";
		public int StructureID { get => m_structureID; set => m_structureID = value; }
		private int m_structureID;

		public GameObject StructureObject => gameObject;

		public int MaxCapacity => m_maxCapacity;
		[SerializeField] private int m_maxCapacity = 4;

		public int ActorsAssigned => m_actorsAssigned;
		[SerializeField] private int m_actorsAssigned = 0;

		public override bool UseFormationRadius { get => false; }

		private void Awake()
		{
			if (m_cachedHousingBT == null)
			{
				BehaviourTree tree = new BehaviourTree();
				BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
				{

				});
				tree.SetTree(root);
				m_cachedHousingBT = tree;
			}
		}

		public override void UpdateSpeed(int extra)
		{

		}

		public void AssignActor(out ActorHouseIO structure)
		{
			if (ActorsAssigned < MaxCapacity)
			{
				m_actorsAssigned++;
			}

			structure = this;
		}

		public override BehaviourTree GetBehaviourTree() => m_cachedHousingBT;
	}
}