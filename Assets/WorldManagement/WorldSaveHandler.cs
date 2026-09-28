using SaveLoad.Core;
using SaveLoad.Data;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WorldManagement.Core
{
    public class WorldSaveHandler : MonoBehaviour
    {
		private void OnEnable()
		{
			WorldManager.OnRequestChunkData += FetchChunkData;
			WorldManager.OnReleaseChunkData += SaveAndUnloadChunkData;
			SaveEvents.SavingBegan += SaveAllActiveChunks;
		}

		private void OnDestroy()
		{
			WorldManager.OnRequestChunkData -= FetchChunkData;
			WorldManager.OnReleaseChunkData -= SaveAndUnloadChunkData;
			SaveEvents.SavingBegan -= SaveAllActiveChunks;
		}

		private TerrainChunk FetchChunkData(Vector2Int chunkXZ)
		{
			string path = SaveUtility.GetChunkFilePath(chunkXZ);

			SerializableChunkData chunkData = SaveLoadManager.Instance.LoadData<SerializableChunkData>(path);

			if (chunkData == null)
				return null;

			TerrainChunk chunk = new TerrainChunk(chunkXZ);
			if (chunkData.SavableEntities != null &&
				chunkData.SavableEntities.Count > 0)
			{
				chunk.PendingSavables =
					chunkData.SavableEntities;
			}

			return chunk;
		}

		private void SaveAndUnloadChunkData(TerrainChunk chunk)
		{
			if (chunk == null) 
				return;

			string path = SaveUtility.GetChunkFilePath(chunk.ChunkXZ);
			SerializableChunkData dataToSave = new SerializableChunkData(chunk);
			SaveLoadManager.Instance.SaveData(path, dataToSave);
		}

		public void SaveAllActiveChunks()
		{
			TerrainChunk[] activeChunks = new TerrainChunk[WorldManager.s_ActiveChunks.Count];

			int index = 0;
			foreach (var pair in WorldManager.s_ActiveChunks)
			{
				activeChunks[index++] = pair.Value.chunkData;
			}

			foreach (TerrainChunk chunk in activeChunks)
			{
				SaveAndUnloadChunkData(chunk);
			}
		}
	}
}
