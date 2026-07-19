using GenericIndex;
using UnityEngine;

namespace SaveLoad.Data
{
	[CreateAssetMenu(fileName = "SaveData", menuName = "SaveData/SavableEntityPrefabDataIndex")]
	public class SavableEntityIndex : GenericIndexBase<SavableEntityPrefabData>
	{
		public SavableEntityPrefabData[] SavableEntityPrefabData => assets;
	}
}
