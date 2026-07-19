using GenericIndex;
using UnityEngine;

namespace InventorySystem.Items
{
	[CreateAssetMenu(fileName = "ItemIndex", menuName = "Items/ItemIndex")]
	public class ItemIndex : GenericIndexBase<ItemData> { }
}