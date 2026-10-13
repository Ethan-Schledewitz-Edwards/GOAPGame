using BehaviourTrees;
using Interaction.Core;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TreeHealthComponent))]
public class HarvestableActorInteractable : ActorInteractableBase
{
	private static BehaviourTree m_harvestBT;

	protected override int m_minActorsToFunction => 1;

	// IInteractable properties
	protected override string m_interactPrompt => string.Empty;
	protected override bool m_hasTrackingDisplayInfo => true;

	protected override void Awake()
	{
		base.Awake();

		InitializeBehaviourTree();
	}

	private void InitializeBehaviourTree()
	{
		if (m_harvestBT != null)
			return;

		BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
		{
			new MoveToInteractionPointTask(),
			new AttackTask()
		});

		BehaviourTree tree = new BehaviourTree();
		tree.SetTree(root);

		m_harvestBT = tree;
	}

	public override bool TryInteract(IInteractor interactor,
			Vector3 actorPosition,
			InteractionPoint reservedPosition)
	{
		if (!base.TryInteract(interactor, actorPosition, reservedPosition))
			return false;

		return true;
	}

	public override BehaviourTree GetBehaviourTree() => m_harvestBT;
}
