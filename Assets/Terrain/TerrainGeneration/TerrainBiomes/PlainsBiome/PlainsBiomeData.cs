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

	public override int GenerateTileData(int seed, int terrainHeight, int worldY)
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

		return 0;
	}
}
