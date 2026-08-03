using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace SaveLoad.Core
{
	public class SaveLoadManager : MonoBehaviour
	{
		public static SaveLoadManager Instance { get; private set; }

		private void Awake()
		{
			if (Instance == null) 
				Instance = this;
			else 
				Destroy(gameObject);
		}

		/// <summary>
		/// Serializes and saves the specified data to a file at the given absolute path.
		/// </summary>
		public void SaveData<T>(string savePath, T data)
		{
			try
			{
				string directory = Path.GetDirectoryName(savePath);
				if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

				using (FileStream stream = new FileStream(savePath, FileMode.Create))
				{
					BinaryFormatter formatter = new BinaryFormatter();
					formatter.Serialize(stream, data);
				}
			}
			catch (Exception e)
			{
				Debug.LogError($"Failed to save to {savePath}: {e.Message}");
			}
		}

		/// <summary>
		/// Loads and deserializes an object of type T from the specified file path.
		/// </summary>
		public T LoadData<T>(string SavePath) where T : class
		{
			if (!File.Exists(SavePath)) 
				return null;

			try
			{
				using (FileStream stream = new FileStream(SavePath, FileMode.Open))
				{
					// Ignore empty files
					if (stream.Length == 0) 
						return null;

					BinaryFormatter formatter = new BinaryFormatter();
					return formatter.Deserialize(stream) as T;
				}
			}
			catch (Exception e)
			{
				Debug.LogError($"Failed to load from {SavePath}: {e.Message}");
				return null;
			}
		}
	}
}