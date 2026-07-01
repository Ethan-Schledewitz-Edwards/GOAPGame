using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class TerrainChunkUtilities
{
	#region Directions

	public static Vector2Int[] GetCardinalDirections = new[]
	{
		Vector2Int.up,// North
		Vector2Int.right,// East
		Vector2Int.down,// South
		Vector2Int.left,// West
	};

	public static Vector3Int[] GetCardinalIntercardinalDirections = new Vector3Int[]
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

	public static Vector3Int[] GetAllDirections = new Vector3Int[]
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
	#endregion

	#region Positioning

	/// <summary>
	/// Returns the chunkXZ of a given worldspace coordinate.
	/// </summary>
	public static Vector2Int WorldToChunkXZ(Vector3Int worldPosition)
	{
		Vector3Int chunkSize = WorldBuilder.s_ChunkSize;

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
		Vector3Int chunkSize = WorldBuilder.s_ChunkSize;

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
		Vector3Int chunkSize = WorldBuilder.s_ChunkSize;

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
		Vector3Int chunkSize = WorldBuilder.s_ChunkSize;

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
	#endregion

	#region Neighbour Tiles

	/// <summary>
	/// Determines whether a neighboring tile lies inside this chunk or across into another.
	/// </summary>
	public static bool IsNeighborTileInChunk(Vector2Int chunkXZ, int[,,] chunkTiles, Vector3Int localPosition, Vector3Int direction, out Vector2Int neighbourXZ)
	{
		neighbourXZ = chunkXZ;
		Vector3Int localOffset = localPosition + direction;

		// Check if the neighbor tile is within the same chunk.
		if (IsLocalPositionInChunk(chunkTiles, localOffset))
		{
			return true;
		}

		// If not in the same chunk, calculate its world position and check the neighbor chunk.
		Vector3Int worldPosition = TileToWorldspace(chunkXZ, localPosition);
		Vector3Int neighborWorldPosition = worldPosition + direction;
		Vector2Int neighbourChunkXZ = WorldToChunkXZ(neighborWorldPosition);

		if (WorldBuilder.s_ActiveChunks.TryGetValue(neighbourChunkXZ, out (TerrainChunk chunk, GameObject chunkObject) value))
		{
			Vector3Int localNeighbourPosition = WorldToTile(neighbourChunkXZ, neighborWorldPosition);
			int[,,] neighborTileData = value.chunk.TileData;

			if(IsLocalPositionInChunk(neighborTileData, localNeighbourPosition))
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
		if (localOffset.y >= WorldBuilder.s_ChunkSize.y ||
			localOffset.y < 0)
			return false;

		// Get the nighbours ID
		if (IsLocalPositionInChunk(chunkTiles, localOffset))
		{
			neighboursTileID = chunkTiles[localOffset.x, localOffset.y, localOffset.z];	
		}
		else
		{
			// Find potential neighbour in another loaded chunk 
			Vector3Int worldPosition = TileToWorldspace(chunkXZ, localPosition);
			Vector3Int neighborWorldPosition = worldPosition + direction;
			Vector2Int neighbourChunkXZ = WorldToChunkXZ(neighborWorldPosition);

			if (WorldBuilder.s_ActiveChunks.TryGetValue(neighbourChunkXZ, out var neighborChunk))
			{
				Vector3Int localNeighbourPosition = WorldToTile(neighbourChunkXZ, neighborWorldPosition);
				int[,,] neighborTileData = neighborChunk.chunkData.TileData;

				// Ensure the calculated local position is valid for the neighbor chunk before accessing it
				if (IsLocalPositionInChunk(neighborTileData, localNeighbourPosition))
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
	#endregion

	// Move this to a chunk utilites class and rename this one to tile utils

	public static Vector2Int[] GetChunkCoordinatesInRadius(Vector3 worldPosition, int checkRadius)
	{
		int playerX = (int)(worldPosition.x / WorldBuilder.s_ChunkSize.x);
		int playerZ = (int)(worldPosition.z / WorldBuilder.s_ChunkSize.z);

		HashSet<Vector2Int> surroundingChunks = new HashSet<Vector2Int>();

		// Fetch chunks in a spiral
		int i = 0, j = 0;
		int di = 1, dj = 0;
		int segmentLength = 1;
		int segmentPassed = 0;
		int maxChunks = (2 * checkRadius + 1) * (2 * checkRadius + 1);

		for (int k = 0; k < maxChunks; ++k)
		{
			Vector2Int chunkCoordinate = new Vector2Int(playerX + i, playerZ + j);
			surroundingChunks.Add(chunkCoordinate);

			i += di; j += dj; segmentPassed++;
			if (segmentPassed == segmentLength)
			{
				segmentPassed = 0;
				int temp = di; di = -dj; dj = temp;
				if (dj == 0) segmentLength++;
			}
		}

		return surroundingChunks.ToArray();
	}
}
