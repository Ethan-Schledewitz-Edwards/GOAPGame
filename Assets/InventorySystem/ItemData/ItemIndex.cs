using GenericIndex;
using UnityEngine;

namespace InventorySystem.Items
{
	[CreateAssetMenu(fileName = "ItemIndex", menuName = "Items/ItemIndex")]
	public class ItemIndex : GenericIndex<ItemData> 
	{
		public ItemData[] Items => Assets;
	}
}