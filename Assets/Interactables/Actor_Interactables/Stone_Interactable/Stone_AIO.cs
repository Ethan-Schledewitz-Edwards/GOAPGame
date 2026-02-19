using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

public class Stone_AIO : ActorInteractableObject_Base
{
	public override void Interact(Actor actor)
	{
		
	}

	public override void StopInteract()
	{
		throw new System.NotImplementedException();
	}

	public override void UpdateSpeed(int extra)
	{
		throw new System.NotImplementedException();
	}

	public override BehaviourTree GetBehaviourTree(Transform userTransform)
	{
		BehaviourTree tree = new BehaviourTree();

		BTNode root = new BTSelector(new List<BTNode>
		{
			new BTSequence(new List<BTNode>
			{
				new CheckForTargetTask(userTransform),
				new DestroyTask(userTransform)
			})
		});

		root.SetData("target", this.transform);

		tree.SetTree(root);

		return tree;
	}
}
