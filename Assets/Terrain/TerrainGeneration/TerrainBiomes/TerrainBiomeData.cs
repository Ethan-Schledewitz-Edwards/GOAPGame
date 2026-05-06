using UnityEngine;

public abstract class TerrainBiomeData : ScriptableObject
{
	public abstract int Floor { get; } // Minimum height

	public abstract int Peak { get; } // Maximum height

	public abstract float Amplitude { get; } // Controls the height of the terrain

	public abstract float Frequency { get; } // Controls the scale of features

	public abstract int Octaves { get; } // Number of noise layers stacked together (more octaves add detail at smaller scales)

	public abstract float Persistence { get; } // Controls how much influence each octave has (lower values make higher-frequency detail contribute less)

	public abstract float Lacunarity { get; } // Controls how quickly the frequency increases for each octave (lower values result in smoother transitions

	/// <summary>
	/// Uses perlin noise along with terrain generation parameters to determine the ground height
	/// </summary>
	public int GetTerrainHeight(int seed, float x, float z)
	{
		float total = 0f;
		float amplitude = Amplitude;
		float frequency = Frequency;
		float totalAmplitude = 0;

		int floor = Floor;
		int peak = Peak;
		int octaves = Octaves;

		// Octaves
		System.Random rng = new System.Random(seed);
		Vector2[] octaveOffsets = new Vector2[octaves];
		for (int i = 0; i < octaves; i++)
		{
			float offsetX = rng.Next(-100000, 100000);
			float offsetY = rng.Next(-100000, 100000);
			octaveOffsets[i] = new Vector2(offsetX, offsetY);
		}

		// Sample noise
		for (int i = 0; i < octaves; i++)
		{
			float xSample = (x * frequency) + octaveOffsets[i].x;
			float ySample = (z * frequency) + octaveOffsets[i].y;
			float noiseSample = Mathf.PerlinNoise(xSample, ySample);
			total += noiseSample * amplitude;

			totalAmplitude += amplitude;

			amplitude *= Persistence;
			frequency *= Lacunarity;
		}

		// Normalize total to between zero and one
		total /= totalAmplitude;

		// Remap normalized value to desired height range of biome
		float final = total * (peak - floor) + floor;

		return (int)final;
	}

	public abstract int GetTileData(int seed, int terrainHeight, int worldX, int worldY, int worldZ);

	public abstract int TryFeatureTile(int seed, int terrainHeight, int worldX, int worldY, int worldZ);

	// <summary>
	/// Generates a deterministic random float between 0.0 and 1.0 based on a set of coordinates
	/// </summary>
	protected float PerCoordinateRandom(int seed, int x, int y, int z)
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
}
