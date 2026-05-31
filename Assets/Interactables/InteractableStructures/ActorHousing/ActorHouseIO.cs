using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

public class ActorHouseIO : InteractableObjectBase, IInteractableStructure<ActorHouseIO>
{
	private static BehaviourTree m_cachedHousingBT;

	[SerializeField] private float m_maxCapacity = 4f;
	[SerializeField] private float m_actorsAssigned = 0f;
	public float MaxCapacity => m_maxCapacity;
	public float ActorsAssigned => m_actorsAssigned;
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
