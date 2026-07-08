using GenericIndex;
using UnityEngine;

[CreateAssetMenu(fileName = "BlueprintIndex", menuName = "Blueprints/BlueprintIndex")]
public class BlueprintDataIndex : GenericIndexBase<StructureBlueprintData> 
{
	public StructureBlueprintData[] StructureBlueprintData => Assets;
}
