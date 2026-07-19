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
	public bool IsItemStored => m_isItemStored;
	private bool m_isItemStored;
	public override bool UseFormationRadius { get => false; }

	public void Awake()
	{
		m_rb = GetComponent<Rigidbody>();

		if (m_ItemBT == null)
		{
			BehaviourTree tree = new BehaviourTree();

			StructureTag blueprintTag = IndexRegistry.GetAsset<StructureTag>("Blueprint_StructureTag");
			StructureTag storageTag = IndexRegistry.GetAsset<StructureTag>("Storage_StructureTag");

			BTNodeBase findUseTask = new FindUseForItemTask(blueprintTag, storageTag);
			BTTimeoutNode timeoutSearch = new BTTimeoutNode(findUseTask, 2f);

			BTNodeBase depositTask = new DepositHeldItemTask();
			BTTimeoutNode timeoutDeposit = new BTTimeoutNode(depositTask, 2f);

			BTNodeBase jobTask = new AquireJobFromTargetTask();
			BTTimeoutNode timeoutJobSearch = new BTTimeoutNode(jobTask, 2f);

			BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
			{
				timeoutSearch,
				new MoveToTargetDataTask(),
				new CheckForTargetRangeTask(),
				timeoutDeposit,
				timeoutJobSearch
			});
			tree.SetTree(root);
			m_ItemBT = tree;
		}
	}

	public override bool TryInteract(IInteractor interactor, bool interactionTakesPriority)
	{
		AssignActor();
		interactor.OnInteractWithObject(this, interactionTakesPriority);

		if (m_itemData == null)
			return false;

		// Add to actor inventory
		if(interactor.Transform.TryGetComponent(out InventoryComponent inventoryComponent))
		{
			if (inventoryComponent.Inventory == null)
				return false;

			Transform[] itemTransform =
			{
				transform
			};
			bool isItemAdded = inventoryComponent.TryAddItem(m_itemData, StackSize, itemTransform);
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

	public void HandleItemStored(Transform parent)
	{
		Debug.Log("Item Stored");

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
		Debug.Log("Item Dropped");

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

	public override BehaviourTree GetBehaviourTree() => m_ItemBT;
}
