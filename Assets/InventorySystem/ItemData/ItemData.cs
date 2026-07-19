using GenericIndex;
using ObjectTags;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem.Items
{
	[CreateAssetMenu(fileName = "ItemData", menuName = "Items/ItemData")]
	public class ItemData : ScriptableObject, IIndexedAsset, ITaggable<ItemTag>
	{
		[field: SerializeField] public int ItemID { get; private set; }
		[field: SerializeField] public string ItemName { get; private set; }
		[field: SerializeField] public int MaxStackSize { get; private set; } = 100;
		[field: SerializeField] public GameObject ItemPrefab { get; private set; }

		[SerializeField] private ItemTag[] m_itemTags;

		private HashSet<ItemTag> m_itemTagCache;
		HashSet<ItemTag> ITaggable<ItemTag>.RuntimeTagSet
		{
			get
			{
				if (m_itemTagCache == null) m_itemTagCache = new HashSet<ItemTag>(m_itemTags);
				return m_itemTagCache;
			}
		}

#if UNITY_EDITOR
		public void SetID(int newID)
		{
			ItemID = newID;
		}
#endif
	}
}
