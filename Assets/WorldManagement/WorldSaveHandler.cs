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

		private void OnDisable()
		{
			WorldManager.OnRequestChunkData -= FetchChunkData;
			WorldManager.OnReleaseChunkData -= SaveAndUnloadChunkData;

			SaveEvents.SavingBegan -= SaveAllActiveChunks;
		}

		private TerrainChunk FetchChunkData(Vector2Int chunkXZ)
		{
			string path = SaveUtility.GetChunkFilePath(chunkXZ);

			SerializableChunkData chunkData = 
				SaveLoadManager.Instance.LoadData<SerializableChunkData>(path);
			if (chunkData != null)
			{
				TerrainChunk chunk = new TerrainChunk(
					chunkXZ,
					chunkData.TileData,
					chunkData.BiomeMap
				);
				chunk.SetGenerationState(chunkData.GenerationState);

				if (chunkData.SavableEntities != null && chunkData.SavableEntities.Count > 0)
				{
					chunk.PendingSavables = chunkData.SavableEntities;
				}
				return chunk;
			}

			return null; // Generation fallback
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
			foreach (var kvp in WorldManager.s_ActiveChunks.Keys)
			{
				if (WorldManager.s_ActiveChunks.TryGetValue(kvp, out var activeChunkTuple))
				{
					if (activeChunkTuple.chunkData.ChunkGenerationState == TerrainChunk.EChunkGenerationState.Decorated)
					{
						SaveAndUnloadChunkData(activeChunkTuple.chunkData);
					}
				}
			}
		}
	}
}
