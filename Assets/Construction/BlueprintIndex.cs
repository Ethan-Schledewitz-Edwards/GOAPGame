using GenericIndex;
using UnityEngine;

[CreateAssetMenu(fileName = "BlueprintIndex", menuName = "Blueprints/BlueprintIndex")]
public class BlueprintIndex : GenericIndexBase<BlueprintData> 
{
	public BlueprintData[] Blueprints => Assets;
}
