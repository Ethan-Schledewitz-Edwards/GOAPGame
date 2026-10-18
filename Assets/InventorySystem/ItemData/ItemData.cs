using AssetIndex.Core;
using ObjectTags;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem.Items
{
	[CreateAssetMenu(fileName = "ItemData", menuName = "Items/ItemData")]
	public class ItemData : ScriptableObject, IIndexedAsset, ITaggable<ResourceTypeTag>
	{
		public string ID => name;
		[field: SerializeField] public string DisplayName { get; private set; }
		[field: SerializeField] public int MaxStackSize { get; private set; } = 100;
		[field: SerializeField] public GameObject ItemPrefab { get; private set; }

		[SerializeField] private ResourceTypeTag[] m_itemTags;

		private HashSet<ResourceTypeTag> m_itemTagCache;
		HashSet<ResourceTypeTag> ITaggable<ResourceTypeTag>.RuntimeTagSet
		{
			get
			{
				if (m_itemTagCache == null) m_itemTagCache = new HashSet<ResourceTypeTag>(m_itemTags);
				return m_itemTagCache;
			}
		}
	}
}
