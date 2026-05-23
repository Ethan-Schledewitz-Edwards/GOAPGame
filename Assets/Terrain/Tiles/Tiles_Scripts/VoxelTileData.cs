using UnityEngine;

[CreateAssetMenu(fileName = "VoxelTileData", menuName = "Tiles/VoxelTileData")]
public class VoxelTileData : TileDataBase
{
	[field: SerializeField] public Material TileMaterial;
}
