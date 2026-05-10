using System;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "RollingHillsBiome", menuName = "Biomes/RollingHillsBiome")]
public class RollingHillsBiomeData : TerrainBiomeData
{
	public override int Floor => 1;

	public override int Peak => 12;

	public override float Amplitude => 24.0f;

	public override float Frequency => 0.02f;

	public override int Octaves => 3;

	public override float Persistence => 0.7f;

	public override float Lacunarity => 2.0f;

	public override int GenerateTileData(int seed, int terrainHeight, int worldY)
	{
		// Grass
		if (worldY == terrainHeight) 
			return 1;

		// Dirt
		if (worldY < terrainHeight && worldY > terrainHeight - 4) 
			return 1;

		// Stone
		if (worldY <= terrainHeight - 4 && worldY > 0) 
			return 1;

		// Bedrock
		if (worldY == 0) 
			return 1;

		return 0;
	}
}
