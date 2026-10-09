using BehaviourTrees;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TreeHealthComponent : HealthComponent
{
	[Header("Settings")]
	[SerializeField] private float m_maxRandomTorqueOnSpawnedLog = 0.2f;

	[Header("Tree")]
	[SerializeField] private GameObject m_treeMeshObject;
	[SerializeField] private GameObject m_stumpMeshObject;

	[Header("Carryable")]
	[SerializeField] private GameObject m_carryablePrefab;// What gets spawned when a harvestable is broken
	[SerializeField] private Vector3 m_localCarryableSpawnOffset;

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

	protected override void OnDie()
	{
		if(TryGetComponent(out Collider collider))
		{
			collider.enabled = false;
		}

		if (m_treeMeshObject != null)
			m_treeMeshObject.SetActive(false);

		if (m_stumpMeshObject != null)
			m_stumpMeshObject.SetActive(true);

		if(m_carryablePrefab != null)
		{
			Vector3 spawnPosition = transform.position + m_localCarryableSpawnOffset;
			GameObject log = Instantiate(m_carryablePrefab, spawnPosition, Quaternion.identity, null);

			if (log.TryGetComponent(out Rigidbody logRigidbody))
			{
				Vector3 randomTorque = new Vector3
				(
				   Random.Range(-m_maxRandomTorqueOnSpawnedLog, m_maxRandomTorqueOnSpawnedLog),
				   Random.Range(-m_maxRandomTorqueOnSpawnedLog, m_maxRandomTorqueOnSpawnedLog),
				   Random.Range(-m_maxRandomTorqueOnSpawnedLog, m_maxRandomTorqueOnSpawnedLog)
				);

				logRigidbody.AddTorque(randomTorque, ForceMode.Impulse);
			}
		}
	}
}
