using System;
using UnityEngine;

namespace TerrainGeneration.Core
{
	[CreateAssetMenu(fileName = "RollingHillsBiome", menuName = "Biomes/RollingHillsBiome")]
	public class RollingHillsBiome : TerrainBiomeData
	{
		public override int Floor => 1;

		public override int Peak => 12;

		public override float Amplitude => 24.0f;

		public override float Frequency => 2f;

		public override int Octaves => 3;

		public override float Persistence => 0.7f;

		public override float Lacunarity => 2.0f;

		public override int GenerateTileData(int seed, int terrainHeight, int worldY)
		{
			// Grass
			if (worldY == terrainHeight)
				return VoxelTileData[0].TileID;

			// Dirt
			if (worldY < terrainHeight && worldY > terrainHeight - 4)
				return VoxelTileData[1].TileID;

			// Stone
			if (worldY <= terrainHeight - 4 && worldY > 0)
				return VoxelTileData[2].TileID;

			// Bedrock
			if (worldY == 0)
				return VoxelTileData[3].TileID;

			return -1;
		}
	}
}
