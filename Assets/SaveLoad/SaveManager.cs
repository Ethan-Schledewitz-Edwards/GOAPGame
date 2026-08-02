using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using SaveLoad.Data;
using UnityEngine;
using WorldManagement.Core;

namespace SaveLoad.Management
{
	public class SaveManager : MonoBehaviour
	{
		public static SaveManager Instance;

		private string m_fileName = "save.dat";
		private string m_playerName = "Ethan";

		public static Action<SaveData> GameLoaded;
		public static Action<TerrainChunk, List<EntitySaveData>> ChunkEntitiesLoaded;
		public static Func<EntitySaveData> RequestPlayerData;

		private void Awake()
		{
			if (Instance == null)
				Instance = this;
			else Destroy(Instance);
		}

		private void OnEnable()
		{
			WorldManager.OnRequestChunkData += FetchChunkData;
			WorldManager.OnReleaseChunkData += SaveAndUnloadChunkData;

			LoadGame();
		}

		private void OnDisable()
		{
			WorldManager.OnRequestChunkData -= FetchChunkData;
			WorldManager.OnReleaseChunkData -= SaveAndUnloadChunkData;
		}
		#region File paths

		private string GetSavePath()
		{
			string folderPath = Path.Combine(Application.persistentDataPath, m_playerName);

			// Ensure folder exists
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}

			return Path.Combine(folderPath, m_fileName);
		}

		private string GetChunksDirectoryPath()
		{
			string chunksPath = Path.Combine(Application.persistentDataPath, m_playerName, "Chunks");

			if (!Directory.Exists(chunksPath))
			{
				Directory.CreateDirectory(chunksPath);
			}

			return chunksPath;
		}

		private string GetChunkFilePath(Vector2Int chunkXZ)
		{
			return Path.Combine(GetChunksDirectoryPath(), $"chunk_{chunkXZ.x}_{chunkXZ.y}.dat");
		}
		#endregion

		public void SaveGame()
		{
			string finalPath = GetSavePath();

			EntitySaveData playerData = RequestPlayerData?.Invoke();
			SaveLoadedChunks();

			// Create a file stream
			FileStream stream = new FileStream(finalPath, FileMode.Create);

			DateTime curDateTime = DateTime.Now;
			SaveData saveData = new SaveData(curDateTime, playerData);

			BinaryFormatter formatter = new BinaryFormatter();
			formatter.Serialize(stream, saveData);
			stream.Close();

			Debug.Log($"Saved at {curDateTime} to: {finalPath}");
		}

		private SaveData RetrieveSaveData()
		{
			string finalPath = GetSavePath();

			if (File.Exists(finalPath))
			{
				FileStream stream = new FileStream(finalPath, FileMode.Open);

				BinaryFormatter formatter = new BinaryFormatter();
				SaveData data = formatter.Deserialize(stream) as SaveData;

				stream.Close();

				return data;
			}
			return null;
		}

		public void LoadGame()
		{
			SaveData data = RetrieveSaveData();

			if (data != null)
			{
				Debug.Log($"Loaded save time: {data.SaveTime} from: {GetSavePath()}");
				GameLoaded?.Invoke(data);
			}
			else
			{
				Debug.LogWarning("No save file found.");
				GameLoaded?.Invoke(null);
			}
		}

		private TerrainChunk FetchChunkData(Vector2Int chunkXZ)
		{
			string path = GetChunkFilePath(chunkXZ);

			if (File.Exists(path))
			{
				FileInfo fileInfo = new FileInfo(path);
				if (fileInfo.Length == 0)
				{
					Debug.LogWarning($"Chunk file {chunkXZ} was empty. Deleting to regenerate.");
					File.Delete(path);
					return null;
				}

				try
				{
					using (FileStream stream = new FileStream(path, FileMode.Open))
					{
						BinaryFormatter formatter = new BinaryFormatter();
						SerializableChunkData serializableData = formatter.Deserialize(stream) as SerializableChunkData;

						if (serializableData != null)
						{
							TerrainChunk chunk = new TerrainChunk(serializableData.GetVector2Int(), serializableData.TileData, serializableData.BiomeMap);
							chunk.SetGenerationState(serializableData.GenerationState);

							if (serializableData.SavedEntities != null && serializableData.SavedEntities.Count > 0)
							{
								ChunkEntitiesLoaded?.Invoke(chunk, serializableData.SavedEntities);
							}

							return chunk;
						}
					}
				}
				catch (Exception e)
				{
					Debug.LogError($"Failed to load chunk {chunkXZ}: {e.Message}");
				}
			}
			return null;
		}

		private void SaveAndUnloadChunkData(TerrainChunk chunk)
		{
			if (chunk == null) return;

			string path = GetChunkFilePath(chunk.ChunkXZ);

			try
			{
				SerializableChunkData dataToSave = new SerializableChunkData(chunk);
				using (FileStream stream = new FileStream(path, FileMode.Create))
				{
					BinaryFormatter formatter = new BinaryFormatter();
					formatter.Serialize(stream, dataToSave);
					stream.Flush(true);
				}
			}
			catch (Exception e)
			{
				Debug.LogError($"Failed to save chunk {chunk.ChunkXZ}");
				Debug.LogError(e);
			}
		}

		/// <summary>
		/// Saves all of the active decorated chunks
		/// </summary>
		private void SaveLoadedChunks()
		{
			if (WorldManager.Instance != null)
			{
				var activeKeys = new List<Vector2Int>(WorldManager.s_ActiveChunks.Keys);
				foreach (var key in activeKeys)
				{
					if (WorldManager.s_ActiveChunks.TryGetValue(key, out var activeChunkTuple))
					{
						if (activeChunkTuple.chunkData.ChunkGenerationState == TerrainChunk.EChunkGenerationState.Decorated)
							SaveAndUnloadChunkData(activeChunkTuple.chunkData);
					}
				}
				Debug.Log($"Flushed {activeKeys.Count} active chunks to disk.");
			}
		}

		[System.Serializable]
		public class SaveData
		{
			public DateTime SaveTime;
			public EntitySaveData PlayerData;

			public SaveData(DateTime saveTime, EntitySaveData playerData)
			{
				SaveTime = saveTime;
				PlayerData = playerData;
			}
		}
	}
}