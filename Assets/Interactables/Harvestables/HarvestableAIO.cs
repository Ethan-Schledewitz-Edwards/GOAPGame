using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HarvestableHealthComponent))]
public class HarvestableAIO : ActorInteractableObjectBase
{
	public override bool UseFormationRadius { get => true; }

	public override void Interact(IInteractor interactor)
	{
		base.Interact(interactor);
	}

	public override void StopInteract()
	{
		base.StopInteract();
	}

	public override void UpdateSpeed(int extra) { }

	public override BehaviourTree GetBehaviourTree(Transform actorTransform, BehaviourTreeExecutor behaviourTreeExecutor)
	{
		BehaviourTree tree = new BehaviourTree();

		BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
		{
			//new CheckForTargetRangeTask(actorComponent, userTransform),
			//new HarvestTask(actorComponent)
		});

		root.SetData("targetTransform", transform);

		tree.SetTree(root);

		return tree;
	}
}
