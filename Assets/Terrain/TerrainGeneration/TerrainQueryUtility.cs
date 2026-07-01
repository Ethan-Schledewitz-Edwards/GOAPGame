using UnityEngine;
using Terrain.WorldProperties;

namespace Terrain.Generation
{
    public static class TerrainQueryUtility
    {
		/// <summary>
		/// Determines whether a neighboring tile lies inside this chunk or across into another.
		/// </summary>
		public static bool IsNeighborTileInChunk(Vector2Int chunkXZ, int[,,] chunkTiles, Vector3Int localPosition, Vector3Int direction, out Vector2Int neighbourXZ)
		{
			neighbourXZ = chunkXZ;
			Vector3Int localOffset = localPosition + direction;

			// Check if the neighbor tile is within the same chunk.
			if (CoordinateUtility.IsLocalPositionInChunk(chunkTiles, localOffset))
			{
				return true;
			}

			// If not in the same chunk, calculate its world position and check the neighbor chunk.
			Vector3Int worldPosition = CoordinateUtility.TileToWorldspace(chunkXZ, localPosition);
			Vector3Int neighborWorldPosition = worldPosition + direction;
			Vector2Int neighbourChunkXZ = CoordinateUtility.WorldToChunkXZ(neighborWorldPosition);

			if (WorldBuilder.s_ActiveChunks.TryGetValue(neighbourChunkXZ, out (TerrainChunk chunk, GameObject chunkObject) value))
			{
				Vector3Int localNeighbourPosition = CoordinateUtility.WorldToTile(neighbourChunkXZ, neighborWorldPosition);
				int[,,] neighborTileData = value.chunk.TileData;

				if (CoordinateUtility.IsLocalPositionInChunk(neighborTileData, localNeighbourPosition))
				{
					neighbourXZ = neighbourChunkXZ;
					return false;
				}
			}

			return false;
		}

		/// <summary>
		/// Checks if a neighboring block in a given direction is solid
		/// </summary>
		public static bool IsNeighborTileSolid(Vector2Int chunkXZ, int[,,] chunkTiles, Vector3Int localPosition, Vector3Int direction, out int neighboursTileID)
		{
			Vector3Int localOffset = localPosition + direction;
			neighboursTileID = -1;

			// Ignore positions out of chunk y bounds
			if (localOffset.y >= WorldPropertyUtility.s_ChunkSize.y ||
				localOffset.y < 0)
				return false;

			// Get the nighbours ID
			if (CoordinateUtility.IsLocalPositionInChunk(chunkTiles, localOffset))
			{
				neighboursTileID = chunkTiles[localOffset.x, localOffset.y, localOffset.z];
			}
			else
			{
				// Find potential neighbour in another loaded chunk 
				Vector3Int worldPosition = CoordinateUtility.TileToWorldspace(chunkXZ, localPosition);
				Vector3Int neighborWorldPosition = worldPosition + direction;
				Vector2Int neighbourChunkXZ = CoordinateUtility.WorldToChunkXZ(neighborWorldPosition);

				if (WorldBuilder.s_ActiveChunks.TryGetValue(neighbourChunkXZ, out var neighborChunk))
				{
					Vector3Int localNeighbourPosition = CoordinateUtility.WorldToTile(neighbourChunkXZ, neighborWorldPosition);
					int[,,] neighborTileData = neighborChunk.chunkData.TileData;

					// Ensure the calculated local position is valid for the neighbor chunk before accessing it
					if (CoordinateUtility.IsLocalPositionInChunk(neighborTileData, localNeighbourPosition))
					{
						neighboursTileID = neighborTileData[localNeighbourPosition.x, localNeighbourPosition.y, localNeighbourPosition.z];
					}
				}
				else
					return false;
			}

			// Check if the nighbour is air
			if (neighboursTileID >= 0)
			{
				return true;
			}
			else
				return false;
		}
	}
}
