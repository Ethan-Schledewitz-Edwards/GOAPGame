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

	public override bool TryAddItem(ItemData addedItemData, 
		int amount, 
		Transform[] itemTransforms = null)
	{
		if(addedItemData == null)
			return false;

		// Ignore picking up items of a different type
		string newItemID = addedItemData.ID;
		string heldItemID = HeldItemSlot.SlotsItem != null? HeldItemSlot.SlotsItem.ID : "";
		if (heldItemID != "" && newItemID != heldItemID)
			return false;

		// Try to add the item
		bool wasItemAdded = base.TryAddItem(addedItemData, amount, itemTransforms);

		if (!wasItemAdded)
			return false;

		// Move the item to the held position if physical transforms were provided
		if (itemTransforms != null && itemTransforms.Length > 0)
		{
			Transform itemTransform = itemTransforms[0];
			if (itemTransform.TryGetComponent(out ItemIO item))
			{
				Debug.Log("WHAT DID I BREAK?");
				itemTransform.parent = m_heldItemPosition;
				itemTransform.position = m_heldItemPosition.position;
				item.gameObject.SetActive(true);
			}
		}

		return true;
	}

	private void OnSlotChanged(InventorySlot slot)
	{
		if (slot.AmountInSlot > 0)
			m_behaviourTreeExecutor.AIContext.SetData<string>(AIContextKeys.c_HeldItemID, slot.SlotsItem.ID);
		else
			m_behaviourTreeExecutor.AIContext.ClearData(AIContextKeys.c_HeldItemID);
	}
}

