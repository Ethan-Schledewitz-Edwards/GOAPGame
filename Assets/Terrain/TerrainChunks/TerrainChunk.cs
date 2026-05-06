using UnityEngine;

public class TerrainChunk
{
	public Vector2Int ChunkXZ;
	public int[,,] TileData;

	public System.Action<Vector2Int> OnChunkUpdate;

	public TerrainChunk(Vector2Int chunkXZ, int[,,] tileData)
	{
		ChunkXZ = chunkXZ;
		TileData = tileData;
	}

	public void UpdateChunk()
	{
		OnChunkUpdate?.Invoke(ChunkXZ);
	}

	public void SetTile(Vector3Int localPos, int newID)
	{
		TileData[localPos.x, localPos.y, localPos.z] = newID;
		UpdateChunk();

		// Update neighbour chunks if the updated tile was on a boarder
		Vector3Int[] bitmaskDirs = TerrainChunkUtilities.BitmaskDirections;

		for (int i = 0; i < 4; i++)
		{
			if (!TerrainChunkUtilities.IsNeighborTileInChunk(ChunkXZ, TileData, localPos, bitmaskDirs[i], out Vector2Int neighbourXZ))
			{
				TerrainChunk terrainChunk = WorldBuilder.s_ActiveChunks[neighbourXZ].chunkData;
				terrainChunk.UpdateChunk();
			}
		}
	}

	public int GetTileID(Vector3Int localPos)
	{
		return TileData[localPos.x, localPos.y, localPos.z];
	}
}