using UnityEngine;
using GenericIndex;

namespace WorldManagement.Tiles
{
	public abstract class TileDataBase : ScriptableObject, IIndexedAsset
	{
		[field: SerializeField] public int TileID { get; private set; }

#if UNITY_EDITOR
		public void SetID(int newID)
		{
			TileID = newID;
		}
#endif
	}
}