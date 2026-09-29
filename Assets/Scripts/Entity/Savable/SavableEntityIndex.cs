using AssetIndex.Core;
using UnityEngine;

namespace Entities.Savable
{
	[CreateAssetMenu(fileName = "SaveData", menuName = "SaveData/SavableEntityPrefabDataIndex")]
	public class SavableEntityIndex : AssetIndexBase<SavableEntityPrefabData>
	{
		public SavableEntityPrefabData[] SavableEntityPrefabData => assets;
	}
}
