using InventorySystem;
using InventorySystem.Items;
using System;
using System.Linq;
using UnityEngine;

public class ActorInventory : InventoryComponent
{
	[SerializeField] private Transform m_heldItemPosition;
	[SerializeField] private Transform m_dropItemPosition;

	// Events
	public event Action<Item> OnPickedUpItem;
	public event Action<Item> OnDroppedItem;

	// System
	private int m_interactionLayerMask;
	private InventorySlot m_heldItemSlot;

	#region Monobehaviour Callbacks

	protected override void Awake()
	{
		m_interactionLayerMask = LayerMask.NameToLayer("Interaction");

		InitializeInventory(1);
		m_heldItemSlot = Inventory.Slots[0];
	}

	private void OnEnable()
	{
		Inventory.SlotChanged += OnInventorySlotChanged;
	}

	private void OnDisable()
	{
		Inventory.SlotChanged -= OnInventorySlotChanged;
	}
	#endregion

	public override bool TryAddItem(ItemData addedItemData, int amount, Transform itemTransform = null)
	{
		TryDropHeldItem();

		if (itemTransform != null)
		{
			if(itemTransform.TryGetComponent(out Item item))
			{
				item.ConstrainPhysics(true);
				itemTransform.parent = m_heldItemPosition;
				itemTransform.position = m_heldItemPosition.position;
			}
		}

		return base.TryAddItem(addedItemData, amount, null);
	}

	private void OnInventorySlotChanged(InventorySlot inventorySlot)
	{
		if (inventorySlot.AmountInSlot == 0)
			TryDropHeldItem();
	}

	public void TryDropHeldItem()
	{
		Transform[] allChildren = m_heldItemPosition.GetComponentsInChildren<Transform>().Skip(1).ToArray();
		foreach (Transform child in allChildren)
		{
			if (child != null && child.TryGetComponent(out Item item))
			{
				child.parent = null;
				child.position = m_dropItemPosition.position;

				item.ConstrainPhysics(false);
				child.gameObject.layer = m_interactionLayerMask;
				OnDroppedItem?.Invoke(item);
			}
		}
	}

	public void TryDestroyHeldItem()
	{
		Transform[] allChildren = m_heldItemPosition.GetComponentsInChildren<Transform>().Skip(1).ToArray();
		foreach (Transform child in allChildren)
		{
			if (child != null)
				Destroy(child.gameObject);
		}
	}
}

