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
		private string m_playerName = "Ethan";

		public static Action<TerrainChunk, List<SerializableEntityData>> ChunkEntitiesWereLoaded;

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

		private string GetChunkFilePath(Vector2Int chunkXZ)
		{
			return Path.Combine(Application.persistentDataPath, m_playerName, "Chunks", $"chunk_{chunkXZ.x}_{chunkXZ.y}.dat");
		}

		private TerrainChunk FetchChunkData(Vector2Int chunkXZ)
		{
			string path = GetChunkFilePath(chunkXZ);

			var serializableData = SaveLoadManager.Instance.LoadData<SerializableChunkData>(path);
			if (serializableData != null)
			{
				TerrainChunk chunk = new TerrainChunk(
					serializableData.GetVector2Int(),
					serializableData.TileData,
					serializableData.BiomeMap
				);
				chunk.SetGenerationState(serializableData.GenerationState);

				if (serializableData.ChunkSavables != null && serializableData.ChunkSavables.Count > 0)
				{
					ChunkEntitiesWereLoaded?.Invoke(chunk, serializableData.ChunkSavables);
				}
				return chunk;
			}

			return null; // Generation fallback
		}

		private void SaveAndUnloadChunkData(TerrainChunk chunk)
		{
			if (chunk == null) 
				return;

			string path = GetChunkFilePath(chunk.ChunkXZ);
			SerializableChunkData dataToSave = new SerializableChunkData(chunk);

			SaveLoadManager.Instance.SaveData(path, dataToSave);
		}

		public void SaveAllActiveChunks()
		{
			foreach (var key in WorldManager.s_ActiveChunks.Keys)
			{
				if (WorldManager.s_ActiveChunks.TryGetValue(key, out var activeChunkTuple))
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
