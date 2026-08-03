using System.IO;
using UnityEngine;

namespace SaveLoad.Core
{
	public static class SaveUtility
	{
		// TO DO:
		// Make this dynamic at some point
		public static string CurrentPlayerName = "Ethan";

		public static string GetPlayerFolderPath()
		{
			string path = Path.Combine(Application.persistentDataPath, CurrentPlayerName);

			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			return path;
		}

		public static string GetPlayerSaveFilePath()
		{
			return Path.Combine(GetPlayerFolderPath(), "save.dat");
		}

		public static string GetChunksDirectory()
		{
			string path = Path.Combine(GetPlayerFolderPath(), "Chunks");

			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			return 
				path;
		}

		public static string GetChunkFilePath(Vector2Int chunkXZ)
		{
			return Path.Combine(GetChunksDirectory(), $"chunk_{chunkXZ.x}_{chunkXZ.y}.dat");
		}
	}
}