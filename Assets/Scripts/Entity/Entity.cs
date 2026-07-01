using System;
using UnityEngine;
using Terrain.WorldProperties;

public class Entity : MonoBehaviour
{
	private const float c_moveThreshold = 0.1f;
	private const float c_moveThresholdSqrt = c_moveThreshold * c_moveThreshold;

	// Events
	public event Action EntityPositionChanged;

	// System
	protected Vector3 m_position { get; private set; }
	private Vector2Int m_currentChunkXZ;

	private void FixedUpdate()
	{
		UpdatePosition();
	}

	protected virtual void UpdatePosition()
	{
		float delta = (transform.position - m_position).sqrMagnitude;

		if(delta > c_moveThresholdSqrt)
		{
			m_position = transform.position;
			EntityPositionChanged?.Invoke();

			// Check if the entity entered a new chunk
			Vector2Int chunkXZ = CoordinateUtility.WorldToChunkXZ(m_position);
			if(chunkXZ != m_currentChunkXZ)
			{
				m_currentChunkXZ = chunkXZ;
			}
		}
	}
}