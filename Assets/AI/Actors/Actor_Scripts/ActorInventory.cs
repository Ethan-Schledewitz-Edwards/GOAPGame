using UnityEngine;

public class ActorInventory : InventoryComponent
{
	[SerializeField] private Transform m_heldItemPosition;

	// System
	private InventorySlot m_heldItemSlot;

	#region Monobehaviour Callbacks

	protected override void Awake()
	{
		base.Awake();

		m_heldItemSlot = Inventory.Slots[0];
	}

	private void OnEnable()
	{
		Inventory.OnSlotChanged += UpdateHeldItem;
	}

	private void OnDisable()
	{
		Inventory.OnSlotChanged -= UpdateHeldItem;
	}
	#endregion

	private void UpdateHeldItem(InventorySlot inventorySlot)
	{
		// Destroy any present children
		if(m_heldItemPosition.childCount > 0)
		{
			for (int i = m_heldItemPosition.childCount - 1; i >= 0; i--)
			{
				Destroy(m_heldItemPosition.GetChild(i).gameObject);
			}
		}

		// Create visual if the held slot has an item
		if (inventorySlot == m_heldItemSlot && m_heldItemSlot.SlotsItem != null) 
		{ 
			Item item = Instantiate(m_heldItemSlot.SlotsItem.ItemPrefab, 
				m_heldItemPosition.position, 
				Quaternion.identity, 
				m_heldItemPosition
			);

			item.RB.constraints = RigidbodyConstraints.FreezeAll;

			item.gameObject.layer = 0;
		}
	}
}

