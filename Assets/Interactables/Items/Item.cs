using BehaviourTrees;
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Item : ActorInteractableObjectBase
{
	public override bool UseFormationRadius { get => false; }

	// Components
	private Rigidbody m_rb;

	[Header("Item Data")]
	[field: SerializeField] public ItemData ItemData { get; private set; }
	[field: SerializeField] public int StackSize { get; private set; } = 1;

	// Events
	public Action<Item> OnPickup;

	public void Awake()
	{
		m_rb = GetComponent<Rigidbody>();
	}

	public override BehaviourTree GetBehaviourTree(Transform actorTransform, BehaviourTreeExecutor behaviourTreeExecutor)
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

	public override void Interact(IInteractor interactor)
	{
		base.Interact(interactor);

		if (ItemData == null)
			return;

		// Add to actor inventory
		bool isItemAdded = interactor.InventoryComponent.Inventory.TryAddItem(ItemData, StackSize);

		if(isItemAdded)
			OnPickup?.Invoke(this);
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

	public void ConstrainPhysics(bool isConstrained)
	{
		m_rb.constraints = isConstrained ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.None;
	}
}
