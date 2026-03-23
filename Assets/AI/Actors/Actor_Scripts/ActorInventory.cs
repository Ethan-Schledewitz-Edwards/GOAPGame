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
		
	}
}

