using UnityEngine;
using GenericIndex;

[CreateAssetMenu(fileName = "TileIndex", menuName = "Tiles/TileIndex")]
public class TileIndex : GenericIndex<TileDataBase> 
{
	public TileDataBase[] Tiles => Assets;
}
