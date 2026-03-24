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

	public override BehaviourTree GetBehaviourTree(Transform userTransform, Actor userActorComp)
	{
		BehaviourTree tree = new BehaviourTree();

		BTNodeBase root = new BTSelectorNode(new List<BTNodeBase>
		{
			new BTSequenceNode(new List<BTNodeBase>
			{
				new CheckForTargetRangeTask(userActorComp, userTransform),
				new HarvestTask(userActorComp)
			}),

			// Try and sort the harvested items
			new SortTask(userActorComp)
		});

		root.SetData("targetTransform", transform);

		tree.SetTree(root);

		return tree;
	}
}
