using System.Collections.Generic;
using UnityEngine;

namespace SaveLoad.Data
{
	[System.Serializable]
	public class SerializableEntityData
	{
		public string PrefabKey;

		public string GUID;

		public float PosX;
		public float PosY;
		public float PosZ;

		public float RotX;
		public float RotY;
		public float RotZ;
		public float RotW;

		[SerializeField] 
		public List<ComponentSaveData> ComponentData = new List<ComponentSaveData>();
	}

	[System.Serializable]
	public class ComponentSaveData
	{
		public string K;
		public string V;
	}
}