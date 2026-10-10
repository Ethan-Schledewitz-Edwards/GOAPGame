using BehaviourTrees;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// To do: Make a HarvestableHealthBase when multiple harvestables are added
/// Move most of the functionality there but keep the log spawning logic specific to trees
/// </summary>
public class TreeHealthComponent : HealthComponent
{
	private const float c_spawnTimeout = 0.2f;
	private const float c_logSpawnRadius = 1.8f;
	private const float c_logHalfHeight = .75f; // Probably not the safest way to handle log sizing for the hull check but fuck it we ball.
	private const float c_logColliderRadius = 0.25f;
	private const float c_logHullCastRadius = 0.1f;

	[Header("Tree")]
	[SerializeField] private GameObject m_treeMeshObject;
	[SerializeField] private GameObject m_stumpMeshObject;

	[Header("Carryable")]
	[SerializeField] private GameObject m_spawnedCarryablePrefab;

	// System
	public bool IsDamagable => m_isDamagable;
	[SerializeField] private bool m_isDamagable = true;

	private LayerMask m_logOverlapLayermask;
	private readonly Collider[] m_hitResults = new Collider[8];

	protected override void Awake()
	{
		base.Awake();

		if (m_treeMeshObject != null)
			m_treeMeshObject.SetActive(true);

		if (m_stumpMeshObject != null)
			m_stumpMeshObject.SetActive(false);

		m_logOverlapLayermask = LayerMask.GetMask("Default", "Interaction");
	}

	private void OnDestroy()
	{
		StopAllCoroutines();
	}

	protected override void OnDie()
	{
		if(TryGetComponent(out Collider collider))
		{
			collider.enabled = false;
		}

		// Hide tree then show log
		if (m_treeMeshObject != null)
			m_treeMeshObject.SetActive(false);

		if (m_stumpMeshObject != null)
			m_stumpMeshObject.SetActive(true);

		if(m_spawnedCarryablePrefab != null)
		{
			StartCoroutine(SpawnWithTimeoutRoutine());
		}
	}

	private bool ValidateSpawnPosition(Vector3 targetPosition, 
		Vector3 fallDirection, 
		out Vector3 finalPosition, 
		out Quaternion finalRotation)
	{
		finalPosition = targetPosition;
		finalRotation = Quaternion.identity;

		Vector3 groundCheckStart = targetPosition + Vector3.up * 1;

		// Check for ground to place the log on
		RaycastHit hit;
		if (Physics.Raycast(groundCheckStart, 
			Vector3.down, 
			out hit, 
			2f, 
			m_logOverlapLayermask, 
			QueryTriggerInteraction.Ignore))
		{
			Vector3 slopeDirection = 
				Vector3.ProjectOnPlane(fallDirection, hit.normal).normalized;

			// Logs resting position
			Vector3 spawnPosition = hit.point + (hit.normal * c_logColliderRadius);

			// Hull cast
			Vector3 topPoint = spawnPosition + (slopeDirection * c_logHalfHeight);
			Vector3 bottomPoint = spawnPosition - (slopeDirection * c_logHalfHeight);
			int collidersHit = Physics.OverlapCapsuleNonAlloc(
				topPoint,
				bottomPoint,
				c_logHullCastRadius,
				m_hitResults,
				m_logOverlapLayermask
			);

			// Return the final resting position and rotation of the log
			if(collidersHit == 0)
			{
				finalPosition = spawnPosition;

				finalRotation = 
					Quaternion.LookRotation(slopeDirection, hit.normal);

				return true;
			}
		}

		return false;
	}

	IEnumerator SpawnWithTimeoutRoutine()
	{
		float timeSpawning = 0f;
		while (true)
		{
			// Calculate the logs spawn position
			Vector3 spawnedLogOffset = Random.onUnitSphere * c_logSpawnRadius;
			spawnedLogOffset.y = 0;
			Vector3 spawnPosition = transform.position + spawnedLogOffset;

			// Find the direction which the log fell
			Vector3 logFallDirection = (spawnPosition - transform.position).normalized;

			// Hull cast then place on the ground
			if (ValidateSpawnPosition(spawnPosition, 
				logFallDirection,
				out Vector3 finalPosition,
				out Quaternion finalRotation))
			{
				Instantiate(m_spawnedCarryablePrefab, finalPosition, finalRotation, null);
				break;
			}

			// Increment timer
			timeSpawning += Time.deltaTime;
			if (timeSpawning >= c_spawnTimeout)
			{
				Vector3 fallbackSpawnPosition = transform.position + Vector3.up;
				Instantiate(m_spawnedCarryablePrefab, fallbackSpawnPosition, Quaternion.identity, null);
				break;
			}

			yield return null;
		}
	}
}
