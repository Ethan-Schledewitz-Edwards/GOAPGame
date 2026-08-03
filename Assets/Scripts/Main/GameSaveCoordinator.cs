using SaveLoad.Core;
using SaveLoad.Data;
using System;
using System.IO;
using UnityEngine;

namespace Main.Core
{
	[RequireComponent(typeof(SaveLoadManager))]
	public class GameSaveCoordinator : MonoBehaviour
	{
		private const string c_fileName = "save.dat";
		private const string c_playerName = "Ethan";

		public static GameSaveCoordinator Instance { get; private set; }

		private SaveLoadManager m_saveLoadManager;

		private void Awake()
		{
			if (Instance == null) 
				Instance = this;
			else 
				Destroy(gameObject);

			m_saveLoadManager = GetComponent<SaveLoadManager>();
		}

		private void Start()
		{
			LoadGame();
		}

		private string GetSavePath()
		{
			string folderPath = Path.Combine(Application.persistentDataPath, c_playerName);
			if (!Directory.Exists(folderPath)) 
				Directory.CreateDirectory(folderPath);
			return 
				Path.Combine(folderPath, c_fileName);
		}

		public void SaveGame()
		{
			SaveEvents.SavingBegan?.Invoke();

			// Get the players data
			SerializablePlayerData playerSaveData = SaveEvents.PlayerDataRequested?.Invoke();

			// Write to the disk
			if (playerSaveData != null)
			{
				string finalPath = GetSavePath();
				m_saveLoadManager.SaveData(finalPath, playerSaveData);
				Debug.Log($"Game Saved at {playerSaveData.SaveTime} to {finalPath}");
			}
			else
			{
				Debug.LogWarning("Save failed: No player save data was provided.");
			}
		}

		public void LoadGame()
		{
			string finalPath = GetSavePath();
			SerializablePlayerData loadedData = m_saveLoadManager.LoadData<SerializablePlayerData>(finalPath);

			if (loadedData != null)
			{
				Debug.Log($"Loaded save from {loadedData.SaveTime}");
				SaveEvents.GameLoaded?.Invoke(loadedData);
			}
			else
			{
				Debug.LogWarning("No save file found.");
				SaveEvents.GameLoaded?.Invoke(null);
			}
		}
	}
}