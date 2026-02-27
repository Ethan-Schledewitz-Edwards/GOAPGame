using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class Stone_AIO : ActorInteractableObject_Base
{
    public override void Interact(Actor actor)
    {
        base.Interact(actor);
    }

    public override void StopInteract()
    {
        base.StopInteract();
    }

    public override void UpdateSpeed(int extra)
	{
		
	}

	public override BehaviourTree GetBehaviourTree(Transform userTransform, Actor userActorComp)
	{
		BehaviourTree tree = new BehaviourTree();

		BTNodeBase root = new BTSelectorNode(new List<BTNodeBase>
		{
			new BTSequenceNode(new List<BTNodeBase>
			{
				new CheckForTargetTask(userTransform),
				new HarvestTask(userActorComp)
			})
		});

		root.SetData("target", transform);

		tree.SetTree(root);

		return tree;
	}
}
