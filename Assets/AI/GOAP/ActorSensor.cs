using System;
using UnityEngine;

[RequireComponent (typeof(SphereCollider))]
public class ActorSensor : MonoBehaviour
{
	[SerializeField] private float m_detectionRadius = 3.0f;
	[SerializeField] private float m_timerInterval = 1.0f;

	private SphereCollider m_SensorCollider;

	public event Action OnTargetChanged = delegate { };

	// System

	private GameObject m_target;
	private Vector3 m_targetLastKnownPos;
	float m_timer = 0;

	public Vector3 TargetPosition => m_target? m_target.transform.position : Vector3.zero;
	public bool IsTargetInRange => TargetPosition != Vector3.zero;

	private void Awake()
	{
		m_SensorCollider = GetComponent<SphereCollider>();
		m_SensorCollider.isTrigger = true;
		m_SensorCollider.radius = m_detectionRadius;
	}

	private void Update()
	{
		m_timer += Time.deltaTime;

		if(m_timer > m_timerInterval)
		{
			m_timer = 0;
			UpdateTargetPos(m_target);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!other.CompareTag("Player")) 
			return;

		UpdateTargetPos(other.gameObject);
	}

	private void OnTriggerExit(Collider other)
	{
		if (!other.CompareTag("Player"))
			return;

		UpdateTargetPos();
	}

	private void UpdateTargetPos(GameObject target = null)
	{
		this.m_target = target;
		if (IsTargetInRange && (m_targetLastKnownPos != TargetPosition
			|| m_targetLastKnownPos != Vector3.zero))
		{
			m_targetLastKnownPos = TargetPosition;
			OnTargetChanged?.Invoke();
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = IsTargetInRange ? Color.red : Color.green;
		Gizmos.DrawWireSphere(TargetPosition, m_timer);
	}
}
