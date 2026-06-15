using UnityEngine;
using UISystems.RecyclingScrollRect;
using System.Linq;

public class ConstructionRecyclingScrollRect : RecyclingScrollRectBase<BlueprintData>
{
	[Header("Data")]
	[field: SerializeField] private BlueprintIndex m_blueprintIndex;

	private BlueprintData[] m_cachedData;
	protected override BlueprintData[] m_data => m_cachedData;

	protected override void Awake()
	{
		if (m_blueprintIndex != null && m_blueprintIndex.Blueprints != null)
			m_cachedData = m_blueprintIndex.Blueprints.ToArray();

		// Initialize pool after the data is cached
		base.Awake();
	}
}
