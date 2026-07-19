using GenericIndex;
using ObjectTags;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem.Items
{
	[CreateAssetMenu(fileName = "ItemData", menuName = "Items/ItemData")]
	public class ItemData : ScriptableObject, IIndexedAsset, IObjectTaggable
	{
		[field: SerializeField] public int ItemID { get; private set; }
		[field: SerializeField] public string ItemName { get; private set; }
		[field: SerializeField] public int MaxStackSize { get; private set; } = 100;
		[field: SerializeField] public GameObject ItemPrefab { get; private set; }

		[SerializeField] private ObjectTag[] m_tags;

		private HashSet<ObjectTag> m_tagCache;
		public HashSet<ObjectTag> RuntimeTagSet
		{
			get
			{
				if (m_tagCache == null) m_tagCache = new HashSet<ObjectTag>(m_tags);
				return m_tagCache;
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
