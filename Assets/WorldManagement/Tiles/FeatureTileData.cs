using UnityEngine;

namespace WorldManagement.Tiles
{
	[CreateAssetMenu(fileName = "FeatureTileData", menuName = "Tiles/FeatureTileData")]
	public class FeatureTileData : TileDataBase
	{
		[field: SerializeField] public GameObject Prefab { get; private set; }
	}
}
