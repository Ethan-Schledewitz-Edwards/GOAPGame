using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Terrain.WorldProperties;

namespace Terrain.Generation
{

	/// <summary>
	/// Interprets the block data stored in a given chunk coordinate position.
	/// Using this data, a chunk is built and passed to the world generator.
	/// </summary>
	public class ChunkDataBuilder
	{
		int k_maxIslandRadius = 512;
		int k_oceanFalloff = 400; // The distance away from the center where the land begins to lower into the sea.
		int k_biomeBlendRad = 8;

		[Header("Components")]
		private WorldBuilder m_worldBuilder;
		private BiomeIndex m_biomeIndex;

		private FastNoiseLite noise = new FastNoiseLite();

		[Header("System")]
		private Queue<GeneratingChunk> m_chunkQueue = new Queue<GeneratingChunk>();
		[SerializeField] private bool m_isGenerationEnabled = true;

		public class GeneratingChunk
		{
			public Vector2Int ChunkXZ;
			public System.Action<int[,,], int[,]> OnGenerationComplete;
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

		/// <summary>
		/// Generates a chunks base terrain tile data.
		/// </summary>
		public void GenerateChunkTerrainData(int worldSeed, Vector2Int chunkXZ, int[,,] tileData, int[,] biomeMap)
		{
			Vector3Int chunkSize = WorldPropertyUtility.s_ChunkSize;

			int mapWidth = biomeMap.GetLength(0);
			int mapDepth = biomeMap.GetLength(1);

			// Pre-Calculate biome data
			for (int x = 0; x < mapWidth; x++)
			{
				for (int z = 0; z < mapDepth; z++)
				{
					int worldX = (x - k_biomeBlendRad) + (chunkXZ.x * chunkSize.x);
					int worldZ = (z - k_biomeBlendRad) + (chunkXZ.y * chunkSize.z);
					biomeMap[x, z] = GetBiome(worldSeed, worldX, worldZ);
				}
			}

			// Generate terrain
			for (int x = 0; x < chunkSize.x; x++)
			{
				for (int z = 0; z < chunkSize.z; z++)
				{
					int height = GetTileHeight(worldSeed, chunkXZ, biomeMap, x, z);
					int biomeIndex = biomeMap[x + k_biomeBlendRad, z + k_biomeBlendRad];

					// Set tile data
					for (int y = 0; y < chunkSize.y; y++)
					{
						int tileID = m_biomeIndex.Biomes[biomeIndex].TerrainBiome.GenerateTileData(worldSeed, height, y);
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
			Vector3Int chunkSize = WorldPropertyUtility.s_ChunkSize;
			Vector2Int chunkXZ = centerChunk.ChunkXZ;

			for (int x = 0; x < chunkSize.x; x++)
			{
				for (int z = 0; z < chunkSize.z; z++)
				{
					int height = GetTileHeight(worldSeed, chunkXZ, centerChunk.BiomeMap, x, z);
					int biomeID = centerChunk.BiomeMap[x + k_biomeBlendRad, z + k_biomeBlendRad];

					// Set tile data
					for (int y = height + 1; y < chunkSize.y; y++)
					{
						int tileID = m_biomeIndex.Biomes[biomeID].TerrainBiome.TryGenerateFeatureTileData(worldSeed, centerChunk, height, x, y, z);
						centerChunk.TileData[x, y, z] = tileID;
					}
				}
			}
		}

		/// <summary>
		/// Converts world generation parameters into blocks.
		/// Once blocks are decided, their data is built into a chunk.
		/// </summary>
		private IEnumerator GenerateChunkBaseData(Vector2Int chunkXZ, System.Action<int[,,], int[,]> callback)
		{
			Vector3Int chunkSize = WorldPropertyUtility.s_ChunkSize;
			int worldSeed = WorldBuilder.s_Seed;

			int[,,] tileData = new int[chunkSize.x, chunkSize.y, chunkSize.z];

			int mapWidth = chunkSize.x + (k_biomeBlendRad * 2);
			int mapDepth = chunkSize.z + (k_biomeBlendRad * 2);
			int[,] biomeMap = new int[mapWidth, mapDepth];

			Task t = Task.Run(() =>
			{
				GenerateChunkTerrainData(worldSeed, chunkXZ, tileData, biomeMap);
			});

			yield return new WaitUntil(() => t.IsCompleted);

			if (t.Exception != null)
			{
				Debug.LogError(t.Exception);
				yield break;
			}

			callback(tileData, biomeMap);
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

		private int GetBiome(int worldSeed, float worldX, float worldZ)
		{
			float tempScale = m_biomeIndex.TempuratureMapScale;
			float humidityScale = m_biomeIndex.HumidityMapScale;

			float rawTemp = noise.GetNoise(worldX * tempScale, worldZ * tempScale);
			float temp = (rawTemp * 0.5f) + 0.5f;

			float rawHumidity = noise.GetNoise((worldX + 10000f) * humidityScale, (worldZ + 10000f) * humidityScale); // Offset humidity so it does not overlap with temp
			float humidity = (rawHumidity * 0.5f) + 0.5f;

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

		private int GetTileHeight(int worldSeed, Vector2Int chunkXZ, int[,] biomeMap, int localX, int localZ)
		{
			Vector3Int chunkSize = WorldPropertyUtility.s_ChunkSize;
			int worldX = localX + (chunkXZ.x * chunkSize.x);
			int worldZ = localZ + (chunkXZ.y * chunkSize.z);

			float totalHeight = 0f;
			float totalWeight = 0f;

			int centerX = localX + k_biomeBlendRad;
			int centerZ = localZ + k_biomeBlendRad;

			// Check if a tile requires blending
			int centerBiomeID = biomeMap[centerX, centerZ];
			bool needsBlending = false;
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					if (biomeMap[centerX + i * k_biomeBlendRad, centerZ + j * k_biomeBlendRad] != centerBiomeID)
					{
						needsBlending = true;
						break;
					}
				}
				if (needsBlending) break;
			}

			// Island fallof
			float distFromCenter = Vector2.Distance(new Vector2(worldX, worldZ), Vector2.zero);
			float mask = 1.0f - Mathf.Clamp01((distFromCenter - k_oceanFalloff) / (k_maxIslandRadius - k_oceanFalloff));

			if (needsBlending)
			{
				// Blend height with neighbour block biomes
				for (int x = -k_biomeBlendRad; x <= k_biomeBlendRad; x++)
				{
					for (int z = -k_biomeBlendRad; z <= k_biomeBlendRad; z++)
					{
						int neighbourBiomeID = biomeMap[centerX + x, centerZ + z];
						TerrainBiomeData neighborBiome = m_biomeIndex.Biomes[neighbourBiomeID].TerrainBiome;

						float heightValue = neighborBiome.GetTerrainHeight(worldSeed, worldX, worldZ);

						// Closer neighbours matter more
						float weight = 1f / ((x * x + z * z) + 1f);
						totalHeight += heightValue * weight;
						totalWeight += weight;
					}
				}

				float finalHeight = totalHeight / totalWeight;

				return Mathf.Clamp(Mathf.RoundToInt(finalHeight * mask), 0, chunkSize.y - 1);
			}
			else
				return Mathf.RoundToInt(m_biomeIndex.Biomes[centerBiomeID].TerrainBiome.GetTerrainHeight(worldSeed, worldX, worldZ) * mask);
		}
	}

}