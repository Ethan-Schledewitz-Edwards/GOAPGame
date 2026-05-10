using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Interprets the block data stored in a given chunk coordinate position.
/// Using this data, a chunk is built and passed to the world generator.
/// </summary>
public class ChunkDataBuilder
{
	float k_maxIslandRadius = 512f;
	float k_oceanFalloff = 400f; // The distance away from the center where the land begins to lower into the sea.
	float k_biomeBlendingStrength = 12f;

	[Header("Components")]
	private WorldBuilder m_worldBuilder;
	private BiomeIndex m_biomeIndex;

	[Header("System")]
	private Queue<GeneratingChunk> m_chunkQueue = new Queue<GeneratingChunk>();
	[SerializeField] private bool m_isGenerationEnabled = true;

	public class GeneratingChunk
	{
		public Vector2Int ChunkXZ;
		public System.Action<int[,,]> OnGenerationComplete;
	}

	public ChunkDataBuilder(WorldBuilder worldBuilder, BiomeIndex biomeIndex)
	{
		this.m_worldBuilder = worldBuilder;
		this.m_biomeIndex = biomeIndex;

		worldBuilder.StartCoroutine(BuildChunksFromQueue());
	}

	public void QueueDataToGenerate(GeneratingChunk data)
	{
		m_chunkQueue.Enqueue(data);
	}

	private int GetBiome(int seed, float worldX, float worldZ)
	{
		float tempScale = m_biomeIndex.TempuratureMapScale;
		float humidityScale = m_biomeIndex.HumidityMapScale;

		float temp = Mathf.PerlinNoise((worldX + seed) * tempScale, (worldZ + seed) * tempScale);
		float humidity = Mathf.PerlinNoise((worldX + seed + 1000) * humidityScale, (worldZ + seed + 1000) * humidityScale); // Offset by 1000 so the noise maps are not the same

		int bestIndex = 0;
		float minDiff = float.MaxValue;

		for (int i = 0; i < m_biomeIndex.Biomes.Length; i++)
		{
			var b = m_biomeIndex.Biomes[i];

			// Euclidean distance 
			float dTemp = temp - b.TargetTemperature;
			float dHum = humidity - b.TargetHumidity;
			float diff = (dTemp * dTemp) + (dHum * dHum);

			if (diff < minDiff)
			{
				minDiff = diff;
				bestIndex = i;
			}
		}
		return bestIndex;
	}

	private float GetHumidity(int seed, float worldX, float worldZ)
	{
		float humidityScale = m_biomeIndex.HumidityMapScale;

		float humidity = Mathf.PerlinNoise((worldX + seed) * humidityScale, (worldZ + seed) * humidityScale); // Offset by 1000 so the noise maps are not the same
		return humidity;
	}

	private float GetTemperature(int seed, float worldX, float worldZ)
	{
		float tempScale = m_biomeIndex.TempuratureMapScale;

		float offset = 10000f;
		float temp = Mathf.PerlinNoise((worldX + seed + offset) * tempScale, (worldZ + seed + offset) * tempScale);

		return temp;
	}

	/// <summary>
	/// Generates a chunks base terrain tile data.
	/// </summary>
	public void GenerateChunkTerrainData(int[,,] tileData, Vector2Int chunkXZ, int worldSeed)
	{
		Vector3Int ChunkSize = WorldBuilder.s_ChunkSize;

		for (int x = 0; x < ChunkSize.x; x++)
		{
			for (int z = 0; z < ChunkSize.z; z++)
			{
				var (height, biome) = GetTileData(worldSeed, chunkXZ, x, z);

				// Set tile data
				for (int y = 0; y < ChunkSize.y; y++)
				{
					int tileID = m_biomeIndex.Biomes[biome].TerrainBiome.GenerateTileData(worldSeed, height, y);
					tileData[x, y, z] = tileID;
				}
			}
		}
	}

	/// <summary>
	/// Decorates a surrounded chunk with features dependant on biome
	/// </summary>
	public void DecorateChunk(TerrainChunk centerChunk)
	{
		// Only decorate chunks containing nothing but terrain
		if (centerChunk.ChunkGenerationState != TerrainChunk.EChunkGenerationState.BaseTerrain) 
			return;

		int worldSeed = WorldBuilder.s_Seed;
		Vector3Int ChunkSize = WorldBuilder.s_ChunkSize;
		Vector2Int chunkXZ = centerChunk.ChunkXZ;

		for (int x = 0; x < ChunkSize.x; x++)
		{
			for (int z = 0; z < ChunkSize.z; z++)
			{
				var (height, biome) = GetTileData(worldSeed, chunkXZ, x, z);

				// Set tile data
				for (int y = height + 1; y < ChunkSize.y; y++)
				{
					int tileID = m_biomeIndex.Biomes[biome].TerrainBiome.TryGenerateFeatureTileData(worldSeed, centerChunk, height, x, y, z);
					centerChunk.TileData[x, y, z] = tileID;
				}
			}
		}
	}

	/// <summary>
	/// Converts world generation parameters into blocks.
	/// Once blocks are decided, their data is built into a chunk.
	/// </summary>
	private IEnumerator GenerateChunkBaseData(Vector2Int chunkXZ, System.Action<int[,,]> callback)
	{
		Vector3Int ChunkSize = WorldBuilder.s_ChunkSize;
		int worldSeed = WorldBuilder.s_Seed;

		int[,,] tileData = new int[ChunkSize.x, ChunkSize.y, ChunkSize.z];

		Task t = Task.Run(() =>
		{
			GenerateChunkTerrainData(tileData, chunkXZ, worldSeed);
		});

		yield return new WaitUntil(() => t.IsCompleted);

		if (t.Exception != null)
		{
			Debug.LogError(t.Exception);
			yield break;
		}

		callback(tileData);
	}

	/// <summary>
	/// Builds all of the chunks present in the queue.
	/// </summary>
	private IEnumerator BuildChunksFromQueue()
	{
		while (m_isGenerationEnabled)
		{
			if (m_chunkQueue.Count > 0)
			{
				GeneratingChunk chunk = m_chunkQueue.Dequeue();
				yield return m_worldBuilder.StartCoroutine(GenerateChunkBaseData(chunk.ChunkXZ, chunk.OnGenerationComplete));
			}

			yield return null;
		}
	}

	private (int height, int biomeID) GetTileData(int worldSeed, Vector2Int chunkXZ, int localX, int localZ)
	{
		Vector3Int ChunkSize = WorldBuilder.s_ChunkSize;

		int worldX = localX + (chunkXZ.x * ChunkSize.x);
		int worldZ = localZ + (chunkXZ.y * ChunkSize.z);

		// Get biome
		int biomeID = GetBiome(worldSeed, worldX, worldZ);
		TerrainBiomeData biome = m_biomeIndex.Biomes[biomeID].TerrainBiome;

		float distFromCenter = Vector2.Distance(new Vector2(worldX, worldZ), Vector2.zero);
		float mask = 1.0f - Mathf.Clamp01((distFromCenter - k_oceanFalloff) / (k_maxIslandRadius - k_oceanFalloff));

		int height = Mathf.RoundToInt(biome.GetTerrainHeight(worldSeed, worldX, worldZ) * mask);
		height = Mathf.Clamp(height, 0, ChunkSize.y - 1);

		return (height, biomeID);
	}
}
