using SaveLoad.Data;
using System;
using UnityEngine;

namespace SaveLoad.Core
{
	public static class SaveEvents
	{
		/// <summary>
		/// Broadcasted right before writing to disk.
		/// </summary>
		public static Action SavingBegan;

		/// <summary>
		/// Broadcasted after data is loaded from disk.
		/// </summary>
		public static Action<SerializablePlayerData> GameLoaded;

		/// <summary>
		/// Should be used to request player data from the the player save handler.
		/// </summary>
		public static Func<SerializablePlayerData> PlayerDataRequested;
	}
}