using BehaviourTrees;
using Entities.Core;
using GenericIndex;
using InventorySystem;
using InventorySystem.Items;
using ObjectTags;
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Entity))]
public class ItemIO : InteractableObjectBase, IItemObject
{
	private static BehaviourTree s_ItemBT;

	// Components
	private Entity m_entity;
	private Rigidbody m_rb;

	[Header("Item Data")]
	[SerializeField] private ItemData m_itemData;
	[field: SerializeField] private int m_stackSize = 1;

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
		m_entity = GetComponent<Entity>();
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
		if (!base.TryInteract(interactor, interactionTakesPriority))
			return false;

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
				if (!TryAssignActor())
					return false;

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
		m_entity.EnableDynamicPositionUpdates(false);
		ConstrainPhysics(true);

		gameObject.SetActive(false);

		if (parent != null)
		{
			transform.parent = parent;
			transform.position = parent.position;
		}
	}

	public void HandleItemDropped(Vector3 dropPosition)
	{
		m_isItemStored = false;
		ConstrainPhysics(false);
		ReleaseActor();

		transform.parent = null;
		if (dropPosition != Vector3.zero)
			transform.position = dropPosition;

		gameObject.SetActive(true);
		m_entity.EnableDynamicPositionUpdates(true);
	}

	private void ConstrainPhysics(bool isConstrained)
	{
		m_rb.constraints = isConstrained ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.None;
	}

	private void InitializeBehaviourTree()
	{
		if (s_ItemBT != null)
			return;

		StructureTag blueprintTag = IndexRegistry.GetAsset<StructureTag>("Blueprint_StructureTag");
		StructureTag storageTag = IndexRegistry.GetAsset<StructureTag>("Storage_StructureTag");

		BTNodeBase findUseTask = new FindUseForItemTask(blueprintTag, storageTag);
		BTTimeoutNode timeoutSearch = new BTTimeoutNode(findUseTask, 2f);

		BTNodeBase depositTask = new DepositHeldItemTask();
		BTTimeoutNode timeoutDeposit = new BTTimeoutNode(depositTask, 2f);

		BTNodeBase jobTask = new AquireJobFromTargetTask();
		BTTimeoutNode timeoutJobSearch = new BTTimeoutNode(jobTask, 2f);

		BehaviourTree tree = new BehaviourTree();
		BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
			{
				timeoutSearch,
				new MoveToTargetDataTask(),
				new CheckForDestinationRangeTask(),
				timeoutDeposit,
				timeoutJobSearch
			});
		tree.SetTree(root);
		s_ItemBT = tree;
	}
}
