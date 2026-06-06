using System;

namespace InventorySystem.Items
{
	[System.Serializable]
	public struct ItemQuantity
	{
		public ItemData itemType;
		public int amount;
	}
}