using UnityEngine;
using UISystems.RecyclingScrollRect;
using System.Linq;

public class ConstructionRecyclingScrollRect : RecyclingScrollRectBase<StructureData>
{
	[Header("Data")]
	[field: SerializeField] private StructureIndex m_structureIndex;

	private StructureData[] m_cachedData;
	protected override StructureData[] m_data => m_cachedData;

	private void Awake()
	{
		if (m_structureIndex != null && m_structureIndex.Structures != null)
			m_cachedData = m_structureIndex.Structures.ToArray();
	}
}
