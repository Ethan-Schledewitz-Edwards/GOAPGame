using AssetIndex.Core;
using UnityEngine;

namespace Construction
{
	[CreateAssetMenu(fileName = "BlueprintIndex", menuName = "Blueprints/BlueprintIndex")]
	public class BlueprintDataIndex : AssetIndexBase<BlueprintData>
	{
		public BlueprintData[] StructureBlueprintData => assets;
	}
}
