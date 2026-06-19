using BehaviourTrees;
using System;
using System.Collections.Generic;
using UnityEngine;
using InventorySystem.Items;
using InventorySystem;

[RequireComponent(typeof(Rigidbody))]
public class Item : InteractableObjectBase
{
	private static BehaviourTree m_ItemBT;

	// Components
	private Rigidbody m_rb;

	[Header("Item Data")]
	[field: SerializeField] public ItemData ItemData { get; private set; }
	[field: SerializeField] public int StackSize { get; private set; } = 1;

	// Events
	public Action<Item> ItemPickedUp;

	// System
	public override bool UseFormationRadius { get => false; }

	public void Awake()
	{
		m_rb = GetComponent<Rigidbody>();

		if (m_ItemBT == null)
		{
			BehaviourTree tree = new BehaviourTree();

			BTNodeBase findUseTask = new FindUseForItemTask();
			BTTimeoutNode timeoutSearch = new BTTimeoutNode(findUseTask, 10f, "Timeout");
			BTNodeBase depositTask = new DepositTask();
			BTTimeoutNode timeoutDeposit = new BTTimeoutNode(depositTask, 5f, "Timeout");
			BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
			{
				timeoutSearch,
				new MoveToTargetDataTask(),
				new CheckForTargetRangeTask(),
				timeoutDeposit
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
		if(interactor.Transform.TryGetComponent(out InventoryComponent inventoryComponent))
		{
			if (inventoryComponent.Inventory == null)
				return;

			bool isItemAdded = inventoryComponent.TryAddItem(ItemData, StackSize, transform);
			if (isItemAdded)
			{
				BehaviourTreeExecutor executor = interactor.Transform.GetComponent<BehaviourTreeExecutor>();
				if (executor != null)
				{
					executor?.AIContext.SetData<int>("HeldItemID", ItemData.ItemID);
				}

				ItemPickedUp?.Invoke(this);
			}
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
