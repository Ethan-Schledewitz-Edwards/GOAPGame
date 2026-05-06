using System;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "PlainsBiome", menuName = "Biomes/PlainsBiome")]
public class PlainsBiomeData : TerrainBiomeData
{
	public override int Floor => 1;

	public override int Peak => 6;

	public override float Amplitude => 4.0f;

	public override float Frequency => 0.01f;

	public override int Octaves => 3;

	public override float Persistence => 0.5f;

	public override float Lacunarity => 2.0f;

	public override int GetTileData(int seed, int terrainHeight, int worldX, int worldY, int worldZ)
	{
		// Grass
		if (worldY == terrainHeight)
			return 1;

		// Dirt
		if (worldY < terrainHeight && worldY > terrainHeight - 4)
			return 2;

		// Stone
		if (worldY <= terrainHeight - 4 && worldY > 0)
			return 3;

		// Bedrock
		if (worldY == 0)
			return 4;

		// Either feature or air
		return TryFeatureTile(seed, terrainHeight, worldX, worldY, worldZ);
	}

	public override int TryFeatureTile(int seed, int terrainHeight, int worldX, int worldY, int worldZ)
	{
		if (worldY == terrainHeight + 1) // Only spawn one above the height
		{
			// Convert the hash result to a 0.0 - 1.0 float
			float spawnChance = PerCoordinateRandom(seed, worldX, worldY, worldZ);

			if (spawnChance < 0.05f)
			{
				return (spawnChance < 0.025f) ? 5 : 6;
			}
		}

		return 0; // No feature
	}
}
