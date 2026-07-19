using GenericIndex;
using UnityEngine;

namespace Construction
{
	[CreateAssetMenu(fileName = "BlueprintIndex", menuName = "Blueprints/BlueprintIndex")]
	public class BlueprintDataIndex : GenericIndexBase<StructureBlueprintData>
	{
		public StructureBlueprintData[] StructureBlueprintData => assets;
	}
}
