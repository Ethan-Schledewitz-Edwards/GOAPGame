using UnityEngine;

[System.Serializable]
public class SerializableChunkData
{
	public int ChunkX;
	public int ChunkZ;
	public int[,,] TileData;
	public int[,] BiomeMap;
	public TerrainChunk.EChunkGenerationState GenerationState;

	public SerializableChunkData(TerrainChunk chunk)
	{
		ChunkX = chunk.ChunkXZ.x;
		ChunkZ = chunk.ChunkXZ.y;
		TileData = chunk.TileData;
		BiomeMap = chunk.BiomeMap;
		GenerationState = chunk.ChunkGenerationState;
	}

	public Vector2Int GetVector2Int()
	{
		return new Vector2Int(ChunkX, ChunkZ);
	}
}