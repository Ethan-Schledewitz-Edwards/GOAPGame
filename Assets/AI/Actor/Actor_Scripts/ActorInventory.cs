using System;
using System.Linq;
using UnityEngine;
using InventorySystem;

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
		base.Awake();

		m_interactionLayerMask = LayerMask.NameToLayer("Interaction");

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

	private void OnInventorySlotChanged(InventorySlot inventorySlot)
	{
		TryDropHeldItem();

		// Create visual if the held slot has an item
		if (inventorySlot == m_heldItemSlot && m_heldItemSlot.SlotsItem != null) 
		{ 
			GameObject itemObject = Instantiate(m_heldItemSlot.SlotsItem.ItemPrefab, 
				m_heldItemPosition.position, 
				Quaternion.identity, 
				m_heldItemPosition
			);

			if (itemObject.TryGetComponent(out Item item))
			{
				item.ConstrainPhysics(true);
				OnPickedUpItem?.Invoke(item);
			}
		}
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

