using GenericIndex;
using UnityEngine;

namespace Entities.Savable
{
	[CreateAssetMenu(fileName = "SaveData", menuName = "SaveData/SavableEntityPrefabDataIndex")]
	public class SavableEntityIndex : GenericIndexBase<SavableEntityPrefabData>
	{
		public SavableEntityPrefabData[] SavableEntityPrefabData => assets;
	}
}
