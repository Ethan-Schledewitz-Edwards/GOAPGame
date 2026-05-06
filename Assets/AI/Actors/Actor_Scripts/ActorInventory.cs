using System.Linq;
using UnityEngine;

public class ActorInventory : InventoryComponent
{
	[SerializeField] private Transform m_heldItemPosition;
	[SerializeField] private Transform m_dropItemPosition;

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
		Inventory.OnSlotChanged += UpdateHeldItem;
	}

	private void OnDisable()
	{
		Inventory.OnSlotChanged -= UpdateHeldItem;
	}
	#endregion

	private void UpdateHeldItem(InventorySlot inventorySlot)
	{
		TryDropHeldItem();

		// Destroy any present children
		if (m_heldItemPosition.childCount > 0)
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

			item.ConstrainPhysics(false);
			item.gameObject.layer = 0;
		}
	}

	public void TryDropHeldItem()
	{
		Transform[] allChildren = m_heldItemPosition.GetComponentsInChildren<Transform>().Skip(1).ToArray();
		foreach (Transform child in allChildren)
		{
			Debug.Log(child.name);
			if (child != null && child.TryGetComponent(out Item item))
			{
				child.parent = null;
				child.position = m_dropItemPosition.position;

				item.ConstrainPhysics(true);
				child.gameObject.layer = m_interactionLayerMask;
			}
		}
	}

	public void TryDestroyHeldItem()
	{
		Transform[] allChildren = m_heldItemPosition.GetComponentsInChildren<Transform>().Skip(1).ToArray();
		foreach (Transform child in allChildren)
		{
			Debug.Log(child.name);
			if (child != null && child.TryGetComponent(out Item item))
			{
				Destroy(child.gameObject);
			}
		}
	}
}

