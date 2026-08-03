using System;
using UnityEngine;

namespace SaveLoad.Data
{
	[System.Serializable]
	public class SerializablePlayerData
	{
		public DateTime SaveTime;
		public SerializableEntityData PlayerData;

		public SerializablePlayerData(DateTime saveTime, SerializableEntityData playerData)
		{
			SaveTime = saveTime;
			PlayerData = playerData;
		}
	}
}