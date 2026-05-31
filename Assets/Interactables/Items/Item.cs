using BehaviourTrees;
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Item : ActorInteractableObjectBase
{
	private static BehaviourTree m_ItemBT;

	// Components
	private Rigidbody m_rb;

	[Header("Item Data")]
	[field: SerializeField] public ItemData ItemData { get; private set; }
	[field: SerializeField] public int StackSize { get; private set; } = 1;

	// Events
	public Action<Item> OnPickup;

	// System
	public override bool UseFormationRadius { get => false; }

	public void Awake()
	{
		m_rb = GetComponent<Rigidbody>();

		if (m_ItemBT == null)
		{
			BehaviourTree tree = new BehaviourTree();
			BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
			{
				new FindStorageTask(),
				new MoveToTargetDataTask(),
				new CheckForTargetRangeTask(),
				new DepositTask()
			});
			tree.SetTree(root);
			m_ItemBT = tree;
		}
	}

	public override void TryInteract(IInteractor interactor)
	{
		base.TryInteract(interactor);

		if (ItemData == null)
			return;

		// Add to actor inventory
		bool isItemAdded = interactor.InventoryComponent.Inventory.TryAddItem(ItemData, StackSize);

		if (isItemAdded)
		{
			BehaviourTreeExecutor executor = interactor.Transform.GetComponent<BehaviourTreeExecutor>();
			if (executor != null)
			{
				executor?.AIContext.SetData<int>("HeldItemID", ItemData.ItemID);
			}

			OnPickup?.Invoke(this);
		}
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

	public override BehaviourTree GetBehaviourTree() => m_ItemBT;
}
