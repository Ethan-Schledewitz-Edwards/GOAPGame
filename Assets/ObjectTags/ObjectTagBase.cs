using AssetIndex.Core;
using UnityEngine;

namespace ObjectTags
{
	public abstract class ObjectTagBase : ScriptableObject, IIndexedAsset
	{
		[SerializeField] private string m_id;
		public string ID => m_id;

		[SerializeField, TextArea(10, 5)] private string DeveloperNotes;

#if UNITY_EDITOR
		public void SetID(string id) => m_id = id;
#endif
	}
}
