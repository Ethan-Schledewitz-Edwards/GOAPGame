using UnityEngine;
using GenericIndex;

namespace InventorySystem.Items
{
	[CreateAssetMenu(fileName = "ItemData", menuName = "Items/ItemData")]
	public class ItemData : ScriptableObject, IIndexedAsset
	{
		[field: SerializeField] public int ItemID { get; private set; }
		[field: SerializeField] public string ItemName { get; private set; }
		[field: SerializeField] public int MaxStackSize { get; private set; } = 100;
		[field: SerializeField] public GameObject ItemPrefab { get; private set; }

#if UNITY_EDITOR
		public void SetID(int newID)
		{
			ItemID = newID;
		}
#endif
	}
}
