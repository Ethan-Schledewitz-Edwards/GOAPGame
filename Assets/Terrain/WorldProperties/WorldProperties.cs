using UnityEngine;

namespace Terrain.WorldProperties
{
	public static class WorldProperties
	{
		public static readonly Vector3Int s_ChunkSize = new Vector3Int(16, 32, 16);

		public static readonly Vector2Int[] CardinalDirections2D = new[]
		{
			Vector2Int.up,// North
			Vector2Int.right,// East
			Vector2Int.down,// South
			Vector2Int.left,// West
		};

		public static readonly Vector3Int[] CardinalDirections3D = new[]
		{
			Vector3Int.up,// North
			Vector3Int.right,// East
			Vector3Int.down,// South
			Vector3Int.left,// West
		};

		public static readonly Vector3Int[] CardinalIntercardinalDirections3D = new Vector3Int[]
		{
			// Cardinal directions
			Vector3Int.forward,// North
			Vector3Int.right,// East
			Vector3Int.back,// South
			Vector3Int.left,// West

			// Corner directions
			new Vector3Int(1, 0, 1),// North-East
			new Vector3Int(1, 0, -1),// South-East
			new Vector3Int(-1, 0, -1),// South-West
			new Vector3Int(-1, 0, 1),// North-West
		};

		public static readonly Vector3Int[] CardinalIntercardinalDirectionsVertical3D = new Vector3Int[]
		{
			// Cardinal directions
			Vector3Int.forward,// North
			Vector3Int.right,// East
			Vector3Int.back,// South
			Vector3Int.left,// West

			// Corner directions
			new Vector3Int(1, 0, 1),// North-East
			new Vector3Int(1, 0, -1),// South-East
			new Vector3Int(-1, 0, -1),// South-West
			new Vector3Int(-1, 0, 1),// North-West

			Vector3Int.up,// South-West
			Vector3Int.down,// North-West
		};
	}
}
