using GenericIndex;
using UnityEngine;

namespace ObjectTags
{
	public abstract class ObjectTagBase : ScriptableObject, IIndexedAsset
	{
		[field: SerializeField] public int TagID { get; private set; }

		[SerializeField, TextArea(10, 5)] private string DeveloperNotes;

#if UNITY_EDITOR
		public void SetID(int newID)
		{
			TagID = newID;
		}
#endif
	}
}
