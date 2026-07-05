using InventorySystem;
using InventorySystem.Items;
using System;
using System.Linq;
using UnityEngine;

public class ActorInventory : InventoryComponent
{
	[SerializeField] private BehaviourTreeExecutorBase m_behaviourTreeExecutor;
	[SerializeField] private Transform m_heldItemPosition;
	[field: SerializeField] public Transform DropItemTransform { get; private set; }

	// Events
	public event Action<ItemIO> OnPickedUpItem;
	public event Action<ItemIO> OnDroppedItem;

	// System
	public InventorySlot HeldItemSlot { get; private set; }

	#region Monobehaviour Callbacks

	protected override void Awake()
	{
		InitializeInventory(1);
		HeldItemSlot = Inventory.Slots[0];
		HeldItemSlot.SlotUpdated += OnSlotChanged;
	}

	#endregion

	public override bool TryAddItem(ItemData addedItemData, int amount, Transform[] itemTransforms = null)
	{
		if(addedItemData == null)
			return false;

		// Ignore picking up items of a different type
		int newItemID = addedItemData.ItemID;
		int heldItemID = HeldItemSlot.SlotsItem != null? HeldItemSlot.SlotsItem.ItemID : -1;
		if (heldItemID != -1 && newItemID != heldItemID)
			return false;

		// Try to add the item
		bool wasItemAdded = base.TryAddItem(addedItemData, amount, itemTransforms);

		// Move the item to the held position
		Transform itemTransform = itemTransforms[0];
		if (itemTransform != null)
		{
			if(itemTransform.TryGetComponent(out ItemIO item))
			{
				itemTransform.parent = m_heldItemPosition;
				itemTransform.position = m_heldItemPosition.position;
				item.gameObject.SetActive(true);
				return true;
			}
		}

		return false;
	}

	private void OnSlotChanged(InventorySlot slot)
	{
		if(slot.AmountInSlot > 0)
			m_behaviourTreeExecutor.AIContext.SetData<int>(AIContextKeys.c_HeldItemID, slot.SlotsItem.ItemID);
		else m_behaviourTreeExecutor.AIContext.ClearData(AIContextKeys.c_HeldItemID);
	}
}

