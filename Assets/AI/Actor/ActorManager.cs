using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ActorManager : MonoBehaviour
{
	#region Constants

	private const int k_tps = 20;
	private const float k_tpsThreshold = 1.0f / k_tps;

	private const int k_actorOnlineRange = 15;
	private const int k_actorOnlineRangeSqrt = k_actorOnlineRange * k_actorOnlineRange;
	#endregion

	public static ActorManager Instance;

	[SerializeField] private Transform m_spawnoffset;
	[SerializeField] private Actor m_actorPrefab;

	// Events
	public event Action<Actor> FollowingActorLoaded;

	// System
	public static HashSet<Actor> s_Actors = new HashSet<Actor>();
	private List<Actor> m_pendingRemovals = new List<Actor>();
	private bool m_isTicking = false;

	double m_accumulatedTime = 0f;
	private Vector3 m_playerPosition;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		Instance = null;
		s_Actors.Clear();
	}

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;

		s_Actors.Clear();
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
			s_Actors.Clear();
		}
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
		Debug.Log($"There were {offlineSeconds} between save and load");
	}

	public bool TryAddActor(Actor actor, bool loadedFromSaveFile)
	{
		if (!s_Actors.Contains(actor))
		{
			s_Actors.Add(actor);

			if (loadedFromSaveFile &&
				actor.LogicExecutorState == EActorState.STATE_Follow)
				FollowingActorLoaded?.Invoke(actor);

			return true;
		}

		return false;
	}

	public void RemoveActor(Actor actor)
	{
		if (s_Actors.Contains(actor))
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
				s_Actors.Remove(actor);
			}
		}
	}

	private void TickActors(float t)
	{
		m_accumulatedTime += t;

		while (m_accumulatedTime >= k_tpsThreshold)
		{
			m_isTicking = true;
			foreach (Actor actor in s_Actors)
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
					s_Actors.Remove(actor);
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
