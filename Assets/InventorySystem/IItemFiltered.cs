using UnityEngine;
using ObjectTags;

namespace InventorySystem
{
    public interface IItemFiltered
	{
		public ResourceTypeTag[] ItemTagFilter { get; }
	}
}
