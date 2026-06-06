using UnityEngine;
using GenericIndex;

public abstract class TileDataBase : ScriptableObject, IIndexedAsset
{
	[field: SerializeField] public int TileID { get; private set; }

	public void SetID(int newID)
	{
		TileID = newID;
	}
}