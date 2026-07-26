using UnityEngine;
using ObjectTags;

namespace InventorySystem
{
    public interface IItemFiltered
	{
		public ItemTag[] ItemTagFilter { get; }
	}
}
