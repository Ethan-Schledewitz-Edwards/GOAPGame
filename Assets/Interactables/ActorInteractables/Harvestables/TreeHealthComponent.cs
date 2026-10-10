using BehaviourTrees;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// To do: Make a HarvestableHealthBase when multiple harvestables are added
/// Move most of the functionality there but keep the log spawning logic specific to trees
/// </summary>
public class TreeHealthComponent : HealthComponent
{
	private const float c_spawnTimeout = 0.2f;
	private const float c_logSpawnRadius = 1.8f;

	[Header("Tree")]
	[SerializeField] private GameObject m_treeMeshObject;
	[SerializeField] private GameObject m_stumpMeshObject;

	[Header("Carryable")]
	[SerializeField] private GameObject m_spawnedCarryablePrefab;

	// System
	public bool IsDamagable => m_isDamagable;
	[SerializeField] private bool m_isDamagable = true;

	protected override void Awake()
	{
		base.Awake();

		if (m_treeMeshObject != null)
			m_treeMeshObject.SetActive(true);

		if (m_stumpMeshObject != null)
			m_stumpMeshObject.SetActive(false);
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

		// Spawn a log
		if(m_spawnedCarryablePrefab != null)
		{
			GameObject log = Instantiate(m_spawnedCarryablePrefab);
			if(log.TryGetComponent(out LogCarryableActorInteractable logCarryable))
			{
				StartCoroutine(PlaceLogTimeoutRoutine(logCarryable));
			}
			else
			{
				Debug.Log("[TreeHealthComponent] tried spawning a log prefab without a " +
					$"{typeof(LogCarryableActorInteractable)} component.", this);
				Destroy(log);
			}
		}
	}

	
	IEnumerator PlaceLogTimeoutRoutine(LogCarryableActorInteractable logCarryable)
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
			if (logCarryable.ValidateLogPosition(spawnPosition, 
				logFallDirection,
				out Vector3 finalPosition,
				out Quaternion finalRotation))
			{
				logCarryable.transform.position = finalPosition;
				logCarryable.transform.rotation = finalRotation;

				if (logCarryable.TryGetComponent(out NavMeshAgent logAgent))
					logAgent.Warp(finalPosition);

				break;
			}

			// Increment timer
			timeSpawning += Time.deltaTime;
			if (timeSpawning >= c_spawnTimeout)
			{
				Vector3 fallbackSpawnPosition = transform.position + Vector3.up;
				logCarryable.transform.position = fallbackSpawnPosition;
				logCarryable.transform.rotation = Quaternion.identity;

				if (logCarryable.TryGetComponent(out NavMeshAgent logAgent))
					logAgent.Warp(finalPosition);

				break;
			}

			yield return null;
		}
	}
}
