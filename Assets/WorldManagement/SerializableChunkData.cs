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

			Vector2Int thisChunk = new Vector2Int(ChunkX, ChunkZ);

			HashSet<string> savedGuids = new();

			// Populate SavableEntities
			foreach (GameObject entityObj in chunk.ResidentEntities)
			{
				if (entityObj == null)
					continue;

				if (!entityObj.TryGetComponent(out ISavableEntity saveableEntity))
					continue;

				if (!saveableEntity.SavedByChunks)
					continue;

				if (!saveableEntity.IsRegisteredToChunk || 
					saveableEntity.ChunkXZ != thisChunk)
					continue;

				SerializableEntityData data = saveableEntity.GenerateSaveData();

				if (data == null)
					continue;

				if (!savedGuids.Add(data.GUID))
				{
					Debug.LogError($"[SerializableChunkData] Duplicate entity GUID " +
						$"'{data.GUID}' encountered while saving chunk " +
						$"{thisChunk}. Skipping duplicate.");

					continue;
				}

				SavableEntities.Add(data);
			}
		}
	}
}