using WorldManagement.Tiles;
using UnityEngine;
using WorldManagement.Core;

namespace WorldManagement.TerrainGeneration
{
	public abstract class TerrainBiomeData : ScriptableObject
	{
		public abstract int Floor { get; } // Minimum height

		public abstract int Peak { get; } // Maximum height

		public abstract float Amplitude { get; } // Controls the height of the terrain

		public abstract float Frequency { get; } // Controls the scale of features

		public abstract int Octaves { get; } // Number of noise layers stacked together (more octaves add detail at smaller scales)

		public abstract float Persistence { get; } // Controls how much influence each octave has (lower values make higher-frequency detail contribute less)

		public abstract float Lacunarity { get; } // Controls how quickly the frequency increases for each octave (lower values result in smoother transitions

		[field: SerializeField] public VoxelTileData[] VoxelTileData { get; private set; }

		[field: SerializeField] public FeatureWeighting[] FeatureWeightings { get; private set; }

		// System
		private FastNoiseLite terrainNoise;
		private int currentInitializedSeed = int.MinValue;

		/// <summary>
		/// Uses perlin noise along with terrain generation parameters to determine the ground height.
		/// </summary>
		public int GetTerrainHeight(int seed, float worldX, float worldZ)
		{
			if (terrainNoise == null || currentInitializedSeed != seed)
			{
				terrainNoise = new FastNoiseLite();
				currentInitializedSeed = seed;

				terrainNoise.SetSeed(seed);
				terrainNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
				terrainNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
				terrainNoise.SetFractalOctaves(Octaves);
				terrainNoise.SetFractalLacunarity(Lacunarity);
				terrainNoise.SetFractalGain(Persistence);
			}

			float rawNoise = terrainNoise.GetNoise(worldX * Frequency, worldZ * Frequency);
			float normalizedNoise = (rawNoise * 0.5f) + 0.5f;

			float finalHeight = (normalizedNoise * (Peak - Floor)) + Floor;
			return Mathf.RoundToInt(finalHeight);
		}

		public abstract int GenerateTileData(int seed, int terrainHeight, int worldY);

		public int TryGenerateFeatureTileData(int seed, TerrainChunk terrainChunk, int terrainHeight, int localX, int localY, int localZ)
		{
			if (FeatureWeightings == null || FeatureWeightings.Length == 0)
				return -1;

			// Only place features on the surface
			if (localY != terrainHeight + 1)
				return -1;

			Vector3Int localPos = new Vector3Int(localX, localY, localZ);
			bool isPlacementValid = true;

			for (int x = -1; x <= 1; x++)
			{
				for (int z = -1; z <= 1; z++)
				{
					for (int y = -1; y <= 1; y++)
					{
						// Skip the spot where the feature itself will sit
						if (x == 0 && y == 0 && z == 0)
							continue;

						Vector3Int offset = new Vector3Int(x, y, z);
						bool isSolid = TerrainQueryUtility.IsNeighborTileSolid(terrainChunk.ChunkXZ, terrainChunk.TileData, localPos, offset, out _);

						if (y == -1) // The 9 tiles underneath the potential feature
						{
							if (!isSolid)
							{
								isPlacementValid = false;
								break;
							}
						}
						else // The tiles are on the same level or above the potential feature
						{
							if (isSolid)
							{
								isPlacementValid = false;
								break;
							}
						}
					}
					if (!isPlacementValid)
						break;
				}
				if (!isPlacementValid)
					break;
			}

			if (isPlacementValid)
			{
				Vector3Int worldPos = CoordinateUtility.TileToWorldspace(terrainChunk.ChunkXZ, localPos);
				float spawnRoll = PerCoordinateRandom(seed, worldPos.x, worldPos.y, worldPos.z);

				float totalWeight = 0f;
				for (int i = 0; i < FeatureWeightings.Length; i++)
				{
					if (spawnRoll >= FeatureWeightings[i].SpawnThreshold)
					{
						totalWeight += FeatureWeightings[i].SelectionWeight;
					}
				}

				// If no features fit the current coordinate density threshold, return air
				if (totalWeight <= 0f)
					return -1;

				float selectionRoll = PerCoordinateRandom(seed + 9999, worldPos.x, worldPos.y, worldPos.z) * totalWeight;
				float currentWeightSum = 0f;
				for (int i = 0; i < FeatureWeightings.Length; i++)
				{
					if (spawnRoll >= FeatureWeightings[i].SpawnThreshold)
					{
						currentWeightSum += FeatureWeightings[i].SelectionWeight;
						if (selectionRoll <= currentWeightSum)
						{
							return FeatureWeightings[i].FeatureTileData.TileID;
						}
					}
				}
			}

			return -1;
		}

		// <summary>
		/// Generates a deterministic random float between 0.0 and 1.0 based on a set of coordinates.
		/// </summary>
		private float PerCoordinateRandom(int seed, int x, int y, int z)
		{
			// Scramble the input using large primes
			uint h = (uint)seed ^ (uint)x * 73856093u ^ (uint)z * 19349663u ^ (uint)y * 83492791u;

			// MurmurHash
			h ^= h >> 16;
			h *= 0x85ebca6bu;
			h ^= h >> 13;
			h *= 0xc2b2ae35u;
			h ^= h >> 16;

			// Normalize
			return h / (float)uint.MaxValue;
		}

		[System.Serializable]
		public struct FeatureWeighting
		{
			[field: SerializeField] public FeatureTileData FeatureTileData { get; private set; }

			[Tooltip("The relative chance of this feature picking chosen compared to others in the same biome.")]
			[Range(0, 100)] public float SelectionWeight;

			[Tooltip("The minimum noise threshold required for this specific feature to spawn.")]
			[Range(0, 1)] public float SpawnThreshold;
		}
	}
}

