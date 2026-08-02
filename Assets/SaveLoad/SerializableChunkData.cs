using System.Collections.Generic;
using SaveLoad.Data;
using UnityEngine;
using WorldManagement.Core;

namespace SaveLoad.Management
{
	[System.Serializable]
	public class SerializableChunkData
	{
		public int ChunkX;
		public int ChunkZ;
		public int[,,] TileData;
		public int[,] BiomeMap;
		public List<EntitySaveData> SavedEntities = new List<EntitySaveData>();
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
				if (entityObj != null && entityObj.TryGetComponent(out SaveableEntity saveableEntity))
				{
					EntitySaveData data = saveableEntity.GenerateSaveData();
					if (data == null)
						continue;

					SavedEntities.Add(data);
				}
			}
		}

		public Vector2Int GetVector2Int()
		{
			return new Vector2Int(ChunkX, ChunkZ);
		}
	}
}