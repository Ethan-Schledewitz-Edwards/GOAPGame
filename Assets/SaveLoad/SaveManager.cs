using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
	public static SaveManager Instance;

	private string m_savePath = "/save.dat";

	public Action<SaveData> OnGameLoaded;

	private void Awake()
	{
		if (Instance == null)
			Instance = this;
		else Destroy(Instance);
	}

	// Update is called once per frame
	void Update()
    {
		if (Input.GetKeyDown(KeyCode.Tab))
		{
			SaveGame();
		}

		if (Input.GetKeyDown(KeyCode.L))
		{
			LoadGame();
		}
	}

	public void SaveGame()
	{
		string finalPath = Application.persistentDataPath + m_savePath;

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
		string finalPath = Application.persistentDataPath + m_savePath;

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
		string finalPath = Application.persistentDataPath + m_savePath;
		SaveData data = RetrieveSaveData();

		if(data != null)
		{
			Debug.Log($"Loaded at {data.SaveTime} from: {finalPath}");

			OnGameLoaded?.Invoke(data);
		}
	}

	[System.Serializable]
	public class SaveData
	{
		public DateTime SaveTime { get; private set; }

		public SaveData(DateTime saveTime) 
		{
			SaveTime = saveTime;
		}
	}
}
