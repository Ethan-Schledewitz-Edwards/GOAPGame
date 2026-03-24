using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HarvestableHealth))]
public class HarvestableAIO : ActorInteractableObjectBase
{

	public override void Interact(Actor actor)
	{
		base.Interact(actor);
	}

	public override void StopInteract()
	{
		base.StopInteract();
	}

	public override void UpdateSpeed(int extra) { }

	public override BehaviourTree GetBehaviourTree(Transform userTransform, Actor actorComponent)
	{
		BehaviourTree tree = new BehaviourTree();

		BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
		{
			new CheckForTargetRangeTask(actorComponent, userTransform),
			new HarvestTask(actorComponent)
		});

		root.SetData("targetTransform", transform);

		tree.SetTree(root);

		return tree;
	}
}
