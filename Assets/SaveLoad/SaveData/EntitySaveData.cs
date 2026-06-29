using System.Collections.Generic;

namespace SaveLoad.Data
{
	[System.Serializable]
	public class EntitySaveData
	{
		public string GUID;
		public int PrefabId;
		public float PosX, PosY, PosZ;
		public float RotX, RotY, RotZ;

		public Dictionary<string, object> ComponentData = new Dictionary<string, object>();
	}
}