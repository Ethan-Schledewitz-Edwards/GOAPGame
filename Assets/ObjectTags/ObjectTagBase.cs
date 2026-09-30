using AssetIndex.Core;
using UnityEngine;

namespace ObjectTags
{
	public abstract class ObjectTagBase : ScriptableObject, IIndexedAsset
	{
		public string ID => name;

		[SerializeField, TextArea(10, 5)] private string DeveloperNotes;

	}
}
