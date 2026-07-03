using BehaviourTrees;
using System;
using System.Collections.Generic;
using UnityEngine;
using InventorySystem.Items;
using InventorySystem;

[RequireComponent(typeof(Rigidbody))]
public class ItemIO : InteractableObjectBase, IItemObject
{
	private static BehaviourTree m_ItemBT;

	// Components
	private Rigidbody m_rb;

	[Header("Item Data")]
	public ItemData ItemData => m_itemData;
	[SerializeField] private ItemData m_itemData;

	public int StackSize => m_stackSize;
	[field: SerializeField] public int m_stackSize { get; private set; } = 1;

	public Transform Transform => transform;

	// Events
	public event Action<Transform> ItemPickedUp;

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

	public override bool TryInteract(IInteractor interactor)
	{
		base.TryInteract(interactor);

		if (m_itemData == null)
			return false;

		// Add to actor inventory
		if(interactor.Transform.TryGetComponent(out InventoryComponent inventoryComponent))
		{
			if (inventoryComponent.Inventory == null)
				return false;

			bool isItemAdded = inventoryComponent.TryAddItem(m_itemData, StackSize, transform);
			if (isItemAdded)
			{
				ItemPickedUp?.Invoke(transform);
				return true;
			}
		}

		return false;
	}

	public override void StopInteract()
	{
		base.StopInteract();
	}

	public override void UpdateSpeed(int extra){}

	public void SetAmount(int amount)
	{
		m_stackSize = amount;

		if(m_stackSize <= 0)
			Destroy(gameObject);
	}

	public void ConstrainPhysics(bool isConstrained)
	{
		m_rb.constraints = isConstrained ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.None;
	}

	public override BehaviourTree GetBehaviourTree() => m_ItemBT;
}
