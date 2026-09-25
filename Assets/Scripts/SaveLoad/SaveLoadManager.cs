using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace SaveLoad.Core
{
	public class SaveLoadManager : MonoBehaviour
	{
		public static SaveLoadManager Instance { get; private set; }

		// Just got gemini to spit one out lol
		private readonly byte[] m_encryptionKey = new byte[] {
			0x43, 0x87, 0x23, 0x72, 0x11, 0x09, 0x54, 0x81,
			0x19, 0x33, 0x56, 0x88, 0x99, 0x21, 0x64, 0x77
		};

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticData()
		{
			Instance = null;
		}

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;
		}

		private void OnDestroy()
		{
			if (Instance == this)
				Instance = null;
		}

		/// <summary>
		/// Serializes data to JSON and encrypts it using AES.
		/// </summary>
		public void SaveData<T>(string savePath, T data)
		{
			try
			{
				string directory = Path.GetDirectoryName(savePath);
				if (!Directory.Exists(directory))
					Directory.CreateDirectory(directory);

				string json = JsonUtility.ToJson(data);

				using (FileStream stream = new FileStream(savePath, FileMode.Create))
				using (Aes aes = Aes.Create())
				{
					aes.Key = m_encryptionKey;
					aes.GenerateIV();
					stream.Write(aes.IV, 0, aes.IV.Length);

					// Encrypt the JSON string and write it to the file
					using (CryptoStream cryptoStream = new CryptoStream(stream, aes.CreateEncryptor(), CryptoStreamMode.Write))
					using (StreamWriter writer = new StreamWriter(cryptoStream))
					{
						writer.Write(json);
					}
				}
			}
			catch (Exception error)
			{
				Debug.LogError($"Failed to save to {savePath}: {error.Message}");
			}
		}

		/// <summary>
		/// Decrypts the AES file and deserializes the JSON back into an object.
		/// </summary>
		public T LoadData<T>(string savePath) where T : class
		{
			if (!File.Exists(savePath))
				return null;

			try
			{
				using (FileStream stream = new FileStream(savePath, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					using (Aes aes = Aes.Create())
					{
						byte[] iv = new byte[aes.BlockSize / 8]; // 16 bytes for AES

						if (stream.Length < iv.Length)
						{
							Debug.LogError($"Save file at {savePath} is corrupt or incomplete.");
							return null;
						}

						if (!ReadExactly(stream, iv, 0, iv.Length))
						{
							Debug.LogError($"Failed to read full IV from save file: {savePath}");
							return null;
						}

						aes.Key = m_encryptionKey;
						aes.IV = iv;

						using (CryptoStream cryptoStream = new CryptoStream(stream, aes.CreateDecryptor(), CryptoStreamMode.Read))
						using (StreamReader reader = new StreamReader(cryptoStream))
						{
							string json = reader.ReadToEnd();

							// Deserialize into the requested type
							return JsonUtility.FromJson<T>(json);
						}
					}
				}
			}
			catch (Exception error)
			{
				Debug.LogError($"Failed to load from {savePath}: {error.Message}");
				return null;
			}
		}

		private static bool ReadExactly(Stream stream, byte[] buffer, int offset, int count)
		{
			int totalBytesRead = 0;
			while (totalBytesRead < count)
			{
				int bytesRead = stream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
				if (bytesRead == 0)
					return false; // Reached End-of-Stream before reading full buffer

				totalBytesRead += bytesRead;
			}
			return true;
		}
	}
}