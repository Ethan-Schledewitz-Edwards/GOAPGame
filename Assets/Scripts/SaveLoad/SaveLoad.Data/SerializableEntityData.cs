using System.Collections.Generic;
using UnityEngine;

namespace SaveLoad.Data
{
	[System.Serializable]
	public class SerializableEntityData
	{
		public string GUID;
		public int PrefabId;
		public float PosX, PosY, PosZ;
		public float RotX, RotY, RotZ;

		[SerializeField] public Dictionary<string, object> ComponentData = new Dictionary<string, object>();
	}
}