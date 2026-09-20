using System.Collections.Generic;
using SaveLoad.Core;
using SaveLoad.Data;
using UnityEngine;
using WorldManagement.Core;

namespace WorldManagement.Core
{
	[System.Serializable]
	public class SerializableChunkData
	{
		public int ChunkX;
		public int ChunkZ;

		public int SizeX;
		public int SizeY;
		public int SizeZ;

		public int BiomeSizeX;
		public int BiomeSizeZ;

		public int[] TileData;
		public int[] BiomeMap;
		public List<SerializableEntityData> SavableEntities = new List<SerializableEntityData>();
		public TerrainChunk.EChunkGenerationState GenerationState;

		public SerializableChunkData(TerrainChunk chunk)
		{
			ChunkX = chunk.ChunkXZ.x;
			ChunkZ = chunk.ChunkXZ.y;
			GenerationState = chunk.ChunkGenerationState;

			// Extract dimensions assuming 3D array
			SizeX = chunk.TileData.GetLength(0);
			SizeY = chunk.TileData.GetLength(1);
			SizeZ = chunk.TileData.GetLength(2);

			// Flatten TileData
			TileData = new int[SizeX * SizeY * SizeZ];
			for (int x = 0; x < SizeX; x++)
			{
				for (int y = 0; y < SizeY; y++)
				{
					for (int z = 0; z < SizeZ; z++)
					{
						int flatIndex = x + SizeX * (y + SizeY * z);
						TileData[flatIndex] = chunk.TileData[x, y, z];
					}
				}
			}

			// Flatten BiomeMap
			BiomeSizeX = chunk.BiomeMap.GetLength(0);
			BiomeSizeZ = chunk.BiomeMap.GetLength(1);
			BiomeMap = new int[BiomeSizeX * BiomeSizeZ];
			for (int x = 0; x < BiomeSizeX; x++)
			{
				for (int z = 0; z < BiomeSizeZ; z++)
				{
					BiomeMap[x + BiomeSizeX * z] = chunk.BiomeMap[x, z];
				}
			}

			// Populate SavableEntities
			foreach (GameObject entityObj in chunk.ResidentEntities)
			{
				if (entityObj != null && entityObj.TryGetComponent(out ISavableEntity saveableEntity))
				{
					if (!saveableEntity.SavedByChunks)
						continue;

					SerializableEntityData data = saveableEntity.GenerateSaveData();
					if (data == null)
						continue;

					SavableEntities.Add(data);
				}
			}
		}

		public int[,,] GetReconstructedTileData()
		{
			if (TileData == null || TileData.Length == 0)
				return new int[0, 0, 0];

			int[,,] result = new int[SizeX, SizeY, SizeZ];
			for (int x = 0; x < SizeX; x++)
			{
				for (int y = 0; y < SizeY; y++)
				{
					for (int z = 0; z < SizeZ; z++)
					{
						int flatIndex = x + SizeX * (y + SizeY * z);
						result[x, y, z] = TileData[flatIndex];
					}
				}
			}
			return result;
		}

		public int[,] GetReconstructedBiomeMap()
		{
			if (BiomeMap == null || BiomeMap.Length == 0)
				return new int[0, 0];

			int[,] result = new int[BiomeSizeX, BiomeSizeZ];
			for (int x = 0; x < BiomeSizeX; x++)
			{
				for (int z = 0; z < BiomeSizeZ; z++)
				{
					int flatIndex = x + BiomeSizeX * z;
					result[x, z] = BiomeMap[flatIndex];
				}
			}
			return result;
		}
	}
}