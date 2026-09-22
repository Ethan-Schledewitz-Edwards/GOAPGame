using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HarvestableHealthComponent))]
public class HarvestableIO : InteractableObjectBase
{
	private static BehaviourTree m_harvestBT;

	private void Awake()
	{
		if (m_harvestBT != null)
			return;

		BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
		{
			new MoveToInteractionPositionTask(),
			new BTTimeoutNode(new CheckForDestinationRangeTask(), 2f),
			new AttackTask(),
			new BTTimeoutNode(new SearchForClosestJobTask(), 2f),
			new MoveToInteractionPositionTask(),
			new BTTimeoutNode(new CheckForDestinationRangeTask(), 2f),
			new BTTimeoutNode(new AquireNewBehaviourTreeFromTargetTask(), 2f)
		});

		BehaviourTree tree = new BehaviourTree();
		tree.SetTree(root);

		m_harvestBT = tree;
	}

	public override bool TryInteract(IInteractor interactor,
			Vector3 actorPosition,
			InteractionPosition reservedPosition,
			out int interactorValue)
	{
		if (!base.TryInteract(interactor, actorPosition, reservedPosition, out interactorValue))
			return false;

		return true;
	}

	public override void UpdateSpeed(int extra) { }

	public override void StopInteractSpeed() { }

	public override BehaviourTree GetBehaviourTree() => m_harvestBT;
}
