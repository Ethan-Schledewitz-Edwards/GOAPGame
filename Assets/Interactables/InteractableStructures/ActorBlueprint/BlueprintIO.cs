using BehaviourTrees;
using System;
using System.Collections.Generic;
using UnityEngine;

public class BlueprintIO : InteractableObjectBase, IInteractableStructure<BlueprintIO>
{
	private static BehaviourTree m_cachedBlueprintBT;

	public event Action<BlueprintIO> BlueprintCompleted;

	public int SettlementID { get; private set; }

	[SerializeField] private float m_maxCapacity = 4f;
	[SerializeField] private float m_actorsAssigned = 0f;
	public float MaxCapacity => m_maxCapacity;
	public float ActorsAssigned => m_actorsAssigned;
	public override bool UseFormationRadius { get => false; }

	private void Awake()
	{
		if (m_cachedBlueprintBT == null)
		{
			BehaviourTree tree = new BehaviourTree();
			BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
			{
				
			});
			tree.SetTree(root);
			m_cachedBlueprintBT = tree;
		}
	}

	public override void UpdateSpeed(int extra)
	{

	}

	public void AssignActor(out BlueprintIO structure)
	{
		if (ActorsAssigned < MaxCapacity)
		{
			m_actorsAssigned++;
		}

		structure = this;
	}

	public void SetSettlementID(int settlementID)
	{
		SettlementID = settlementID;
	}

	private void CompleteBlueprint()
	{
		BlueprintCompleted?.Invoke(this);
	}

	public override BehaviourTree GetBehaviourTree() => m_cachedBlueprintBT;
}
