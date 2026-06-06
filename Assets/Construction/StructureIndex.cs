using GenericIndex;
using UnityEngine;

[CreateAssetMenu(fileName = "StructureIndex", menuName = "Structures/StructureIndex")]
public class StructureIndex : GenericIndex<StructureData> 
{
	public StructureData[] Structures => Assets;
}
