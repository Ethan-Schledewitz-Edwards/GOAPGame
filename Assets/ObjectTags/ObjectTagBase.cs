using GenericIndex;
using UnityEngine;

namespace ObjectTags
{
	public abstract class ObjectTagBase : ScriptableObject, IIndexedAsset
	{
		[SerializeField] private string m_id;
		public string ID => m_id;

		[SerializeField, TextArea(10, 5)] private string DeveloperNotes;
	}
}
