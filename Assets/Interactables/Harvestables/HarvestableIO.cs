using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HarvestableHealthComponent))]
public class HarvestableIO : InteractableObjectBase
{
	private static BehaviourTree m_HarvestBT;

	private void Awake()
	{
		if(m_HarvestBT == null)
		{
			CheckForDestinationRangeTask checkDestinationTask1 = new CheckForDestinationRangeTask();
			BTTimeoutNode timeoutDestination1 = new BTTimeoutNode(checkDestinationTask1, 2f);

			CheckForDestinationRangeTask checkDestinationTask2 = new CheckForDestinationRangeTask();
			BTTimeoutNode timeoutDestination2 = new BTTimeoutNode(checkDestinationTask2, 2f);

			SearchForInteractionTask searchForInteractionTask = new SearchForInteractionTask();
			BTTimeoutNode timeoutSearch = new BTTimeoutNode(searchForInteractionTask, 2f);

			InteractWithTargetTask interactTask = new InteractWithTargetTask();
			BTTimeoutNode interactTimeout = new BTTimeoutNode(searchForInteractionTask, 2f);

			BehaviourTree tree = new BehaviourTree();
			BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
			{
				new MoveToTargetDataTask(),
				timeoutDestination1,
				new AttackTask(),
				timeoutSearch,
				new MoveToTargetDataTask(),
				timeoutDestination2,
				interactTimeout
			});
			tree.SetTree(root);
			m_HarvestBT = tree;
		}
	}

	public override bool TryInteract(IInteractor interactor,
		Vector3 actorPosition,
		bool interactionTakesPriority,
		InteractionPosition assignedPosition,
		out int interactorValue)
	{
		if (!base.TryInteract(interactor, actorPosition, interactionTakesPriority, assignedPosition, out interactorValue))
			return false;

		interactor.OnInteractWithObject(this, interactionTakesPriority);

		return true;
	}

	public override void UpdateSpeed(int extra) { }

	public override void StopInteractSpeed() { }

	public override BehaviourTree GetBehaviourTree() => m_HarvestBT;
}
