using BehaviourTrees;
using System;
using System.Collections.Generic;
using UnityEngine;
using InventorySystem.Items;
using InventorySystem;
using ObjectTags;
using GenericIndex;

[RequireComponent(typeof(Rigidbody))]
public class ItemIO : InteractableObjectBase, IItemObject
{
	private static BehaviourTree s_ItemBT;

	// Components
	private Rigidbody m_rb;

	[Header("Item Data")]
	[SerializeField] private ItemData m_itemData;
	[field: SerializeField] public int m_stackSize { get; private set; } = 1;

	// Events
	public event Action<Transform> ItemPickedUp;

	// System
	private bool m_isItemStored;

	// IItemObject properties
	public ItemData ItemData => m_itemData;
	public int StackSize => m_stackSize;
	public Transform Transform => transform;
	public bool IsItemStored => m_isItemStored;

	// Base
	public override bool UseFormationRadius { get => false; }

	public void Awake()
	{
		m_rb = GetComponent<Rigidbody>();

		InitializeBehaviourTree();
	}


	public override void StopInteract()
	{
		base.StopInteract();
	}

	public override void UpdateSpeed(int extra) { }

	public override BehaviourTree GetBehaviourTree() => s_ItemBT;

	public override bool TryInteract(IInteractor interactor, bool interactionTakesPriority)
	{
		if (m_itemData == null)
			return false;

		if (interactor.Transform.TryGetComponent(out InventoryComponent inventoryComponent))
		{
			if (inventoryComponent.Inventory == null)
				return false;

			Transform[] itemTransform = { transform };
			bool isItemAdded = inventoryComponent.TryAddItem(m_itemData, StackSize, itemTransform);

			if (isItemAdded)
			{
				AssignActor();
				interactor.OnInteractWithObject(this, interactionTakesPriority);

				ItemPickedUp?.Invoke(transform);
				return true;
			}
		}

		return false;
	}

	public void SetAmount(int amount)
	{
		m_stackSize = amount;

		if (m_stackSize <= 0)
			Destroy(gameObject);
	}


	public void HandleItemStored(Transform parent)
	{
		m_isItemStored = true;
		ConstrainPhysics(true);

		if (parent != null)
		{
			transform.parent = parent;
			transform.position = parent.position;
		}

		gameObject.SetActive(false);
	}

	public void HandleItemDropped(Vector3 dropPosition)
	{
		m_isItemStored = false;
		ConstrainPhysics(false);

		transform.parent = null;
		if (dropPosition != Vector3.zero)
			transform.position = dropPosition;

		gameObject.SetActive(true);
	}

	private void ConstrainPhysics(bool isConstrained)
	{
		m_rb.constraints = isConstrained ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.None;
	}

	private void InitializeBehaviourTree()
	{
		if (s_ItemBT == null)
			return;

		BTNodeBase findUseTask = new FindItemEntityOfTagTask();
		BTTimeoutNode timeoutFind = new BTTimeoutNode(findUseTask, 2f);

		BTNodeBase jobTask = new AquireJobFromTargetTask();
		BTTimeoutNode timeoutJobSearch = new BTTimeoutNode(jobTask, 2f);

		BehaviourTree tree = new BehaviourTree();
		BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
				{
					timeoutFind,
					new MoveToTargetDataTask(),
					new CheckForTargetRangeTask(),
					new InteractWithTargetTask(),
					new ReturnToStructureTask(),
					new MoveToTargetDataTask(),
					new CheckForTargetRangeTask(),
					new DepositHeldItemTask(),
					timeoutJobSearch // Try to loop item search
				});
		tree.SetTree(root);
		s_ItemBT = tree;
	}
}
