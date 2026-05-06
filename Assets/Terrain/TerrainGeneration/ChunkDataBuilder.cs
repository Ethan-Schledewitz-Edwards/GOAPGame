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

		worldBuilder.StartCoroutine(BuildFromQueue());
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
	/// Converts world generation parameters into blocks.
	/// Once blocks are decided, their data is built into a chunk.
	/// </summary>
	private IEnumerator GenerateChunk(Vector2Int chunkXZ, System.Action<int[,,]> callback)
	{
		Vector3Int ChunkSize = WorldBuilder.s_ChunkSize;
		int worldSeed = WorldBuilder.s_Seed;

		int[,,] tileData = new int[ChunkSize.x, ChunkSize.y, ChunkSize.z];

		Task t = Task.Run(() =>
		{
			for (int x = 0; x < ChunkSize.x; x++)
			{
				for (int z = 0; z < ChunkSize.z; z++)
				{
					int worldX = x + (chunkXZ.x * ChunkSize.x);
					int worldZ = z + (chunkXZ.y * ChunkSize.z);

					float distFromCenter = Vector2.Distance(new Vector2(worldX, worldZ), Vector2.zero);
					float mask = 1.0f - Mathf.Clamp01((distFromCenter - k_oceanFalloff) / (k_maxIslandRadius - k_oceanFalloff));

					// Get biome
					int biomeID = GetBiome(worldSeed, worldX, worldZ);
					TerrainBiomeData biome = m_biomeIndex.Biomes[biomeID].TerrainBiome;

					int height = Mathf.RoundToInt(biome.GetTerrainHeight(worldSeed, worldX, worldZ) * mask);
					height = Mathf.Clamp(height, 0, ChunkSize.y - 1);

					// Set tile data
					for (int y = 0; y < ChunkSize.y; y++)
					{
						int tileID = biome.GetTileData(worldSeed, height, worldX, y, worldZ);
						tileData[x, y, z] = tileID;
					}
				}
			}
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
	private IEnumerator BuildFromQueue()
	{
		while (m_isGenerationEnabled)
		{
			if (m_chunkQueue.Count > 0)
			{
				GeneratingChunk chunk = m_chunkQueue.Dequeue();
				yield return m_worldBuilder.StartCoroutine(GenerateChunk(chunk.ChunkXZ, chunk.OnGenerationComplete));
			}

			yield return null;
		}
	}
}
