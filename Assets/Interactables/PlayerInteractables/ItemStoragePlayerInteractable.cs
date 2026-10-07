using InventorySystem;
using InventorySystem.Items;
using UnityEngine;

namespace Interaction.Player
{
    public class ItemStoragePlayerInteractable : PlayerInteractableBase<ItemStoragePlayerInteractable>
    {
		public override bool HasDisplayInfo => true;

		public override bool CanPlayerInteract => false;

		public override string InteractPrompt => "";

		private InventoryComponent m_inventoryComponent;

		private void Awake()
		{
			m_inventoryComponent = GetComponent<InventoryComponent>();
		}

		public void Update()
		{
			UpdateInteractableInfo();
		}

		public (int, ItemData) GetItems()
		{
			return (0, null);
		}
	}
}
