using UnityEngine;
using TerrainGeneration.WorldProperties;

namespace TerrainGeneration.WorldProperties
{
    public static class CoordinateUtility
    {
		/// <summary>
		/// Returns the chunkXZ of a given worldspace coordinate.
		/// </summary>
		public static Vector2Int WorldToChunkXZ(Vector3Int worldPosition)
		{
			Vector3Int chunkSize = WorldPropertyUtility.s_ChunkSize;

			return new Vector2Int
			(
				Mathf.FloorToInt(worldPosition.x / (float)chunkSize.x),
				Mathf.FloorToInt(worldPosition.z / (float)chunkSize.z)
			);
		}

		/// <summary>
		/// Returns the chunkXZ of a given worldspace coordinate.
		/// </summary>
		public static Vector2Int WorldToChunkXZ(Vector3 worldPosition)
		{
			Vector3Int chunkSize = WorldPropertyUtility.s_ChunkSize;

			return new Vector2Int
			(
				Mathf.FloorToInt(worldPosition.x / (float)chunkSize.x),
				Mathf.FloorToInt(worldPosition.z / (float)chunkSize.z)
			);
		}

		/// <summary>
		/// Returns the worldspace position of a given tile within a chunk.
		/// </summary>
		public static Vector3Int TileToWorldspace(Vector2Int chunkXZ, Vector3Int localPosition)
		{
			Vector3Int chunkSize = WorldPropertyUtility.s_ChunkSize;

			return new Vector3Int
			(
				(chunkXZ.x * chunkSize.x) + localPosition.x,
				localPosition.y,
				(chunkXZ.y * chunkSize.z) + localPosition.z
			);
		}

		/// <summary>
		/// Returns the local position of a given tile using the world coordinates and its chunkXZ.
		/// </summary>
		public static Vector3Int WorldToTile(Vector2Int chunkXZ, Vector3Int worldPosition)
		{
			Vector3Int chunkSize = WorldPropertyUtility.s_ChunkSize;

			return new Vector3Int
			(
				worldPosition.x - (chunkXZ.x * chunkSize.x),
				worldPosition.y,
				worldPosition.z - (chunkXZ.y * chunkSize.z)
			);
		}

		/// <summary>
		/// Checks if a position is within the bounds of a 3D array.
		/// </summary>
		public static bool IsLocalPositionInChunk(int[,,] data, Vector3Int localPosition)
		{
			return localPosition.x >= 0 && localPosition.x < data.GetLength(0) &&
				   localPosition.y >= 0 && localPosition.y < data.GetLength(1) &&
				   localPosition.z >= 0 && localPosition.z < data.GetLength(2);
		}
	}
}
