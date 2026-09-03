using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HarvestableHealthComponent))]
public class HarvestableIO : InteractableObjectBase
{
	private static BehaviourTree m_HarvestBT;

	public override bool UseFormationRadius { get => true; }

	private void Awake()
	{
		if(m_HarvestBT == null)
		{
			SearchForInteractionTask searchForInteractionTask = new SearchForInteractionTask();
			BTTimeoutNode timeoutSearch = new BTTimeoutNode(searchForInteractionTask, 2f);

			BehaviourTree tree = new BehaviourTree();
			BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
			{
				new MoveToTargetDataTask(),
				new CheckForDestinationRangeTask(),
				new AttackTask(),
				timeoutSearch
			});
			tree.SetTree(root);
			m_HarvestBT = tree;
		}
	}

	public override void UpdateSpeed(int extra) { }

	public override BehaviourTree GetBehaviourTree() => m_HarvestBT;

	public override bool TryInteract(IInteractor interactor, bool interactionTakesPriority)
	{
		if (!base.TryInteract(interactor, interactionTakesPriority))
			return false;

		if(!TryAssignActor())
			return false;

		interactor.OnInteractWithObject(this, interactionTakesPriority);

		return true;
	}
}
