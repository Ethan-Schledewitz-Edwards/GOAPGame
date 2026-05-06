using UnityEngine;

public static class TerrainChunkUtilities
{
	#region Directions

	/// <summary>
	/// Returns the eight cardinal directions and then up and down.
	/// </summary>
	public static Vector3Int[] BitmaskDirections = new Vector3Int[]
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

		// Up and Down directions
		Vector3Int.up,// Up
		Vector3Int.down// Down
	};

	public static Vector2Int[] CardinalDirections2D = new[]
{
		Vector2Int.up,// North
		Vector2Int.right,// East
		Vector2Int.down,// South
		Vector2Int.left,// West
	};
	#endregion

	#region Positioning

	/// <summary>
	/// Returns the chunkXZ of a given worldspace coordinate.
	/// </summary>
	public static Vector2Int WorldToChunkXZ(Vector3Int worldPos)
	{
		Vector3Int chunkSize = WorldBuilder.s_ChunkSize;

		return new Vector2Int
		(
			Mathf.FloorToInt(worldPos.x / (float)chunkSize.x),
			Mathf.FloorToInt(worldPos.z / (float)chunkSize.z)
		);
	}

	/// <summary>
	/// Returns the worldspace position of a given chunkXZ.
	/// </summary>
	public static Vector3Int ChunkXZToWorld(Vector2Int ChunkXZ)
	{
		Vector3Int chunkSize = WorldBuilder.s_ChunkSize;

		return new Vector3Int
		(
			Mathf.FloorToInt(ChunkXZ.x * (float)chunkSize.x),
			0,
			Mathf.FloorToInt(ChunkXZ.y * (float)chunkSize.z)
		);
	}

	/// <summary>
	/// Returns the worldspace position of a given tile within a chunk.
	/// </summary>
	public static Vector3Int TileToWorldspace(Vector3Int localPos, Vector2Int chunkXZ)
	{
		Vector3Int chunkSize = WorldBuilder.s_ChunkSize;

		return new Vector3Int
		(
			(chunkXZ.x * chunkSize.x) + localPos.x,
			localPos.y,
			(chunkXZ.y * chunkSize.z) + localPos.z
		);
	}

	/// <summary>
	/// Returns the local position of a given tile using the world coordinates and its chunkXZ.
	/// </summary>
	public static Vector3Int WorldToTile(Vector3Int worldPos, Vector2Int chunkXZ)
	{
		Vector3Int chunkSize = WorldBuilder.s_ChunkSize;

		return new Vector3Int
		(
			worldPos.x - (chunkXZ.x * chunkSize.x),
			worldPos.y,
			worldPos.z - (chunkXZ.y * chunkSize.z)
		);
	}

	/// <summary>
	/// Returns the local position of a given tile using the world coordinates.
	/// </summary>
	public static Vector3Int WorldToTile(Vector3Int worldPos)
	{
		Vector2Int chunkXZ = WorldToChunkXZ(worldPos);
		return WorldToTile(worldPos, chunkXZ);
	}

	/// <summary>
	/// Checks if a position is within the bounds of a 3D array.
	/// </summary>
	public static bool IsPosInChunk(int[,,] data, Vector3Int pos)
	{
		return pos.x >= 0 && pos.x < data.GetLength(0) &&
			   pos.y >= 0 && pos.y < data.GetLength(1) &&
			   pos.z >= 0 && pos.z < data.GetLength(2);
	}
	#endregion

	#region Neighbour Tiles

	/// <summary>
	/// Determines whether a neighboring tile lies inside this chunk or across into another.
	/// </summary>
	public static bool IsNeighborTileInChunk(Vector2Int chunkXZ, int[,,] chunkTiles, Vector3Int localPos, Vector3Int dir, out Vector2Int neighbourXZ)
	{
		neighbourXZ = chunkXZ;
		Vector3Int offsetPos = localPos + dir;

		// Check if the neighbor tile is within the same chunk.
		if (IsPosInChunk(chunkTiles, offsetPos))
		{
			return true;
		}

		// If not in the same chunk, calculate its world position and check the neighbor chunk.
		Vector3Int currentWorldPos = TileToWorldspace(localPos, chunkXZ);
		Vector3Int neighborWorldPos = currentWorldPos + dir;
		Vector2Int neighbourChunkXZ = WorldToChunkXZ(neighborWorldPos);

		if (WorldBuilder.s_ActiveChunks.TryGetValue(neighbourChunkXZ, out (TerrainChunk chunk, GameObject chunkObject) value))
		{
			Vector3Int localNeighbourPos = WorldToTile(neighborWorldPos, neighbourChunkXZ);
			int[,,] neighborTileData = value.chunk.TileData;

			if(IsPosInChunk(neighborTileData, localNeighbourPos))
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
	public static bool IsNeighborTileSolid(Vector2Int chunkXZ, int[,,] chunkTiles, Vector3Int localPos, Vector3Int dir)
	{
		Vector3Int offsetPos = localPos + dir;

		// Ignore positions out of chunk y bounds
		if (offsetPos.y >= WorldBuilder.s_ChunkSize.y ||
			offsetPos.y < 0)
			return false;

		// Get the nighbours ID
		int tileID = 0;
		if (IsPosInChunk(chunkTiles, offsetPos))
		{
			tileID = chunkTiles[offsetPos.x, offsetPos.y, offsetPos.z];	
		}
		else
		{
			// Find potential neighbour in another loaded chunk 
			Vector3Int currentWorldPos = TileToWorldspace(localPos, chunkXZ);
			Vector3Int neighborWorldPos = currentWorldPos + dir;
			Vector2Int neighbourChunkXZ = WorldToChunkXZ(neighborWorldPos);

			if (WorldBuilder.s_ActiveChunks.TryGetValue(neighbourChunkXZ, out (TerrainChunk chunk, GameObject chunkObject) value))
			{
				Vector3Int localNeighbourPos = WorldToTile(neighborWorldPos, neighbourChunkXZ);
				int[,,] neighborTileData = value.chunk.TileData;

				// Ensure the calculated local position is valid for the neighbor chunk before accessing it
				if (IsPosInChunk(neighborTileData, localNeighbourPos))
				{
					tileID = neighborTileData[localNeighbourPos.x, localNeighbourPos.y, localNeighbourPos.z];
				}
			}
			else
				return false;
		}

		// Check if the nighbour is air
		if (tileID != 0)
		{
			// Ignore feature tiles
			int tileIndex = tileID - 1;
			if (WorldBuilder.TileIndex.Tiles[tileID - 1] is FeatureTileData featureData)
				return false;

			return true;
		}
		else
			return false;
	}
	#endregion
}
