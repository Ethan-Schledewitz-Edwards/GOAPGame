using UnityEngine;

[CreateAssetMenu(fileName = "TileIndex", menuName = "Tiles/TileIndex")]
public class TileIndex : ScriptableObject
{
	[field: SerializeField] public TileDataBase[] Tiles { get; private set; }
}
