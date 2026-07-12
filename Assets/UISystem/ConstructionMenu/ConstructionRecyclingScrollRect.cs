using UnityEngine;
using UISystems.RecyclingScrollRect;
using System.Linq;
using Construction;

public class ConstructionRecyclingScrollRect : RecyclingScrollRectBase<StructureBlueprintData>
{
	[Header("Data")]
	[field: SerializeField] private BlueprintDataIndex m_blueprintIndex;

	private StructureBlueprintData[] m_cachedData;
	protected override StructureBlueprintData[] m_data => m_cachedData;

	protected override void Awake()
	{
		if (m_blueprintIndex != null && m_blueprintIndex.StructureBlueprintData != null)
			m_cachedData = m_blueprintIndex.StructureBlueprintData.ToArray();

		// Initialize pool after the data is cached
		base.Awake();
	}
}
