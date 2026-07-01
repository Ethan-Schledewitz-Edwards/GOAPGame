using InventorySystem.Items;
using System;
using UnityEngine;

namespace InventorySystem
{
    public interface IItemObject 
    {
		[Header("Item Data")]
		public ItemData ItemData { get; }
		public int StackSize { get; }
		public Transform Transform { get; }

		// Events
		public event Action<Transform> ItemPickedUp;
	}
}
