using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Item : ActorInteractableObjectBase
{
	public Rigidbody RB { get; private set; }

	[field: SerializeField] public ItemData ItemData { get; private set; }
	[field: SerializeField] public int StackSize { get; private set; } = 1;

	public void Awake()
	{
		RB = GetComponent<Rigidbody>();
	}

	public override BehaviourTree GetBehaviourTree(Transform actorTransform, Actor actorComp)
	{
		if(ItemData == null)
			return null;

		BehaviourTree tree = new BehaviourTree();

		BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
		{
			new FindStorageTask(actorComp, ItemData.ItemID),
			new MoveToTargetDataTask(actorComp, actorTransform),
			new CheckForTargetRangeTask(actorComp, actorTransform),
			new DepositTask(actorComp, actorTransform)
		});

		tree.SetTree(root);

		return tree;
	}

	public override void Interact(Actor actor)
	{
		base.Interact(actor);

		if (ItemData == null)
			return;

		// Add to actor inventory
		actor.ActorInventory.Inventory.TryAddItem(this);
	}

	public override void StopInteract()
	{
		base.StopInteract();
	}

	public override void UpdateSpeed(int extra){}

	public void SetAmount(int amount)
	{
		StackSize = amount;

		if(StackSize <= 0)
			Destroy(gameObject);
	}
}
