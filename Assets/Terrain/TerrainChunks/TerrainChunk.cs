using UnityEngine;

public class TerrainChunk
{
	public Vector2Int ChunkXZ { get; private set; }
	public int[,,] TileData { get; private set; }
	public int[,] BiomeMap { get; private set; }

	public EChunkGenerationState ChunkGenerationState { get; private set; }
	public enum EChunkGenerationState
	{
		Empty,
		BaseTerrain,
		Decorated
	}

	public System.Action<Vector2Int> OnChunkUpdate;

	public TerrainChunk(Vector2Int chunkXZ, int[,,] tileData, int[,] biomeMap)
	{
		ChunkXZ = chunkXZ;
		TileData = tileData;
		BiomeMap = biomeMap;

		ChunkGenerationState = EChunkGenerationState.Empty;
	}

	public void SetGenerationState(EChunkGenerationState newGenerationState)
	{
		ChunkGenerationState = newGenerationState;
	}

	public void UpdateChunk()
	{
		OnChunkUpdate?.Invoke(ChunkXZ);
	}

	public void SetTilesID(Vector3Int localPos, int newID)
	{
		TileData[localPos.x, localPos.y, localPos.z] = newID;
		UpdateChunk();

		// Update neighbour chunks if the updated tile was on a boarder
		Vector3Int[] intercadinalDirs = TerrainChunkUtilities.GetCardinalIntercardinalDirections;

		for (int i = 0; i < intercadinalDirs.Length; i++)
		{
			if (!TerrainChunkUtilities.IsNeighborTileInChunk(ChunkXZ, TileData, localPos, intercadinalDirs[i], out Vector2Int neighbourXZ))
			{
				TerrainChunk terrainChunk = WorldBuilder.s_ActiveChunks[neighbourXZ].chunkData;
				terrainChunk.UpdateChunk();
			}
		}
	}
}