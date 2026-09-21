using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ActorManager : MonoBehaviour
{
	public static ActorManager Instance;

	#region Constants

	private const int k_tps = 20;
	private const float k_tpsThreshold = 1.0f / k_tps;

	private const int k_actorOnlineRange = 15;
	private const int k_actorOnlineRangeSqrt = k_actorOnlineRange * k_actorOnlineRange;
	#endregion

	[SerializeField] private Transform m_spawnoffset;
	[SerializeField] private Actor m_actorPrefab;

	private HashSet<Actor> m_actors = new HashSet<Actor>();
	private List<Actor> m_pendingRemovals = new List<Actor>();
	private bool m_isTicking = false;

	double m_accumulatedTime = 0f;
	private Vector3 m_playerPosition;

	private void Awake()
	{
		if (Instance == null)
			Instance = this;
		else Destroy(Instance);
	}

	private void Update()
	{
		TickActors(Time.deltaTime);
	}

	public void SyncSimulation(DateTime lastSave, DateTime now)
	{
		double offlineSeconds = (now - lastSave).TotalSeconds;
		double clampedSeconds = Math.Min(offlineSeconds, 86400); // 24h cap

		Debug.Log($"There were {offlineSeconds} between save and load");
	}

	public void TryAddActor(Actor actor)
	{
		if (!m_actors.Contains(actor))
			m_actors.Add(actor);
	}

	public void RemoveActor(Actor actor)
	{
		if (m_actors.Contains(actor))
		{
			if (m_isTicking)
			{
				// Queue actor for removal later
				if (!m_pendingRemovals.Contains(actor))
				{
					m_pendingRemovals.Add(actor);
				}
			}
			else
			{
				m_actors.Remove(actor);
			}
		}
	}

	private void TickActors(float t)
	{
		m_accumulatedTime += t;

		while (m_accumulatedTime >= k_tpsThreshold)
		{
			m_isTicking = true;
			foreach (Actor actor in m_actors)
			{
				if (actor == null)
					continue;

				if (actor.LogicExecutorState == EActorState.STATE_Follow)
				{
					actor.Pathing.TrySetActorSimFidelity(EPathingSimFidelity.Realtime);
				}
				else
				{
					float distToPlayerSqrt = (m_playerPosition - actor.Transform.position).sqrMagnitude;
					actor.Pathing.UpdateActorSimFidelity(distToPlayerSqrt);
				}

				actor.TickBehaviour(k_tpsThreshold);
			}

			m_isTicking = false;

			// Process all deaths that occurred during this tick
			if (m_pendingRemovals.Count > 0)
			{
				foreach (Actor actor in m_pendingRemovals)
				{
					m_actors.Remove(actor);
				}
				m_pendingRemovals.Clear();
			}

			m_accumulatedTime -= k_tpsThreshold;
		}
	}

	public void SetPlayerPosition(Vector3 playerPosition)
	{
		m_playerPosition = playerPosition;
	}
}
