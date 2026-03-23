using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Item : ActorInteractableObjectBase
{
	[field: SerializeField] public ItemData ItemData { get; private set; }

	public override BehaviourTree GetBehaviourTree(Transform userTransform, Actor userActorComp)
	{
		if(ItemData == null)
			return null;

		BehaviourTree tree = new BehaviourTree();

		BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
		{
			new FindStorageTask(userActorComp, ItemData.ItemID)
		});

		tree.SetTree(root);

		return tree;
	}

	public override void Interact(Actor actor)
	{
		base.Interact(actor);

		if (ItemData == null)
			return;

		// Add to actor inventory slot
		actor.ActorInventory.Inventory.AddItem(ItemData, 1);
	}

	public override void StopInteract()
	{
		base.StopInteract();
	}

	public override void UpdateSpeed(int extra){}
}
