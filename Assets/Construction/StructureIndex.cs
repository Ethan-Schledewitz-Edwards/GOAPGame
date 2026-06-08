using GenericIndex;
using UnityEngine;

[CreateAssetMenu(fileName = "StructureIndex", menuName = "Structures/StructureIndex")]
public class StructureIndex : GenericIndexBase<StructureData> 
{
	public StructureData[] Structures => Assets;
}
