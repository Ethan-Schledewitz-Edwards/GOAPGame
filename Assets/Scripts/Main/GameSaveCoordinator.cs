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

		public void SaveGame()
		{
			SaveEvents.SavingBegan?.Invoke();

			// Get the players data
			SerializablePlayerData playerSaveData = SaveEvents.PlayerDataRequested?.Invoke();

			// Write to the disk
			if (playerSaveData != null)
			{
				string finalPath = SaveUtility.GetPlayerSaveFilePath();
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
			string finalPath = SaveUtility.GetPlayerSaveFilePath();
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