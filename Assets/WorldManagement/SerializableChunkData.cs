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

			Vector2Int thisChunkXZ = new Vector2Int(ChunkX, ChunkZ);

			// Populate SavableEntities
			HashSet<string> savedGuids = new();
			foreach (GameObject entityObj in chunk.ResidentEntities)
			{
				if (entityObj == null)
					continue;

				if (!entityObj.TryGetComponent(out ISavableEntity saveableEntity))
					continue;

				if (!saveableEntity.SavedByChunks)
					continue;

				if (!saveableEntity.IsRegisteredToChunk || 
					saveableEntity.ChunkXZ != thisChunkXZ)
					continue;

				SerializableEntityData data = saveableEntity.GenerateSaveData();

				if (data == null)
					continue;

				if (!savedGuids.Add(data.GUID))
				{
					Debug.LogError($"[SerializableChunkData] Duplicate entity GUID " +
						$"'{data.GUID}' found while saving chunk {thisChunkXZ}. Skipping duplicate.");

					continue;
				}

				SavableEntities.Add(data);
			}
		}
	}
}