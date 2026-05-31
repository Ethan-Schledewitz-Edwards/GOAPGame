using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HarvestableHealthComponent))]
public class HarvestableAIO : ActorInteractableObjectBase
{
	private static BehaviourTree m_HarvestBT;

	public override bool UseFormationRadius { get => true; }

	private void Awake()
	{
		if(m_HarvestBT == null)
		{
			BehaviourTree tree = new BehaviourTree();
			BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
			{
				new CheckForTargetRangeTask(),
				new AttackTask()
			});
			tree.SetTree(root);
			m_HarvestBT = tree;
		}
	}

	public override void Interact(IInteractor interactor)
	{
		base.Interact(interactor);
	}

	public override void StopInteract()
	{
		base.StopInteract();
	}

	public override void UpdateSpeed(int extra) { }

	public override BehaviourTree GetBehaviourTree() => m_HarvestBT;
}
