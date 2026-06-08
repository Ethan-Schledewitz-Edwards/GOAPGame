using UnityEngine;
using GenericIndex;

[CreateAssetMenu(fileName = "TileIndex", menuName = "Tiles/TileIndex")]
public class TileIndex : GenericIndexBase<TileDataBase> 
{
	public TileDataBase[] Tiles => Assets;
}
