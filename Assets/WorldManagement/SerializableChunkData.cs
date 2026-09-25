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

		public List<SerializableEntityData> SavableEntities = new List<SerializableEntityData>();

		public SerializableChunkData(TerrainChunk chunk)
		{
			ChunkX = chunk.ChunkXZ.x;
			ChunkZ = chunk.ChunkXZ.y;

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
	}
}