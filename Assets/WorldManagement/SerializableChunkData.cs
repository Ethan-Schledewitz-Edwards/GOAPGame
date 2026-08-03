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
		public int[,,] TileData;
		public int[,] BiomeMap;
		public List<SerializableEntityData> SavableEntities = new List<SerializableEntityData>();
		public TerrainChunk.EChunkGenerationState GenerationState;

		public SerializableChunkData(TerrainChunk chunk)
		{
			ChunkX = chunk.ChunkXZ.x;
			ChunkZ = chunk.ChunkXZ.y;
			TileData = chunk.TileData;
			BiomeMap = chunk.BiomeMap;
			GenerationState = chunk.ChunkGenerationState;

			foreach (GameObject entityObj in chunk.ResidentEntities)
			{
				if (entityObj != null && entityObj.TryGetComponent(out ISavableEntity saveableEntity))
				{
					SerializableEntityData data = saveableEntity.GenerateSaveData();
					if (data == null)
						continue;

					SavableEntities.Add(data);
				}
			}
		}
	}
}