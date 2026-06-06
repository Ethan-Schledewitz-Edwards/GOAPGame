using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
	public static SaveManager Instance;

	private string m_fileName = "save.dat";
	private string m_playerName = "Ethan";

	public Action<SaveData> OnGameLoaded;

	private void Awake()
	{
		if (Instance == null)
			Instance = this;
		else Destroy(Instance);
	}

	private void OnEnable()
	{
		WorldBuilder.OnRequestChunkData += FetchChunkData;
		WorldBuilder.OnReleaseChunkData += SaveAndUnloadChunkData;
	}

	private void OnDisable()
	{
		WorldBuilder.OnRequestChunkData -= FetchChunkData;
		WorldBuilder.OnReleaseChunkData -= SaveAndUnloadChunkData;
	}

	void Update()
    {
		if (Input.GetKeyDown(KeyCode.F1))
		{
			SaveGame();
		}

		if (Input.GetKeyDown(KeyCode.F2))
		{
			LoadGame();
		}
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

		SaveLoadedChunks();

		// Create a file stream
		FileStream stream = new FileStream(finalPath, FileMode.Create);

		DateTime curDateTime = DateTime.Now;
		SaveData saveData = new SaveData(curDateTime);

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

	private void LoadGame()
	{
		SaveData data = RetrieveSaveData();

		if (data != null)
		{
			Debug.Log($"Loaded save time: {data.SaveTime} from: {GetSavePath()}");
			OnGameLoaded?.Invoke(data);
		}
		else
		{
			Debug.LogWarning("No save file found.");
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
			Debug.LogError($"Failed to save chunk {chunk.ChunkXZ}: {e.Message}");
		}
	}

	/// <summary>
	/// Saves all of the active decorated chunks
	/// </summary>
	private void SaveLoadedChunks()
	{
		if (WorldBuilder.Instance != null)
		{
			var activeKeys = new List<Vector2Int>(WorldBuilder.s_ActiveChunks.Keys);
			foreach (var key in activeKeys)
			{
				if (WorldBuilder.s_ActiveChunks.TryGetValue(key, out var activeChunkTuple))
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

		public SaveData(DateTime saveTime) 
		{
			SaveTime = saveTime;
		}
	}
}
