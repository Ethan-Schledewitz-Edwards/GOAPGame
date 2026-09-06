using System;
using UnityEngine;

namespace InventorySystem.Items
{
    public interface IItemObject 
    {
		[Header("Item Data")]
		public ItemData ItemData { get; }
		public int StackSize { get; }
		public Transform Transform { get; }
		public bool IsItemStored { get; }

		// Events
		public event Action<Transform> ItemPickedUp;

		public void ItemStored(Transform parent);

		public void ItemDropped(Vector3 dropPosition);
	}
}
