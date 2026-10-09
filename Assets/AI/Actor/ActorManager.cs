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

	// Components
	[SerializeField] private Transform m_player;

	// Events
	public event Action<Actor> FollowingActorLoaded;

	// System
	// NOTE: A hash set can be slow to iterate over, it's probably better to use a list.
	// Each actor would track its index in the list. On removal, it's a quick look up,
	// then copy the pointer at the end of the array into the new empty slot and decrease
	// the array size by 1.
	public static HashSet<Actor> s_Actors = new HashSet<Actor>();
	private List<Actor> m_pendingRemovals = new List<Actor>();
	private bool m_isTicking = false;

	double m_accumulatedTime = 0f;

	// NOTE: Caching this value doesn't really make sense because it's used much less frequently
	// than it's updated. I would caution against any form of caching while in the development
	// stage since it requires careful tracking of object lifetimes. Even the	actors list can
	// be replaced by a FindObjectsByType call, which is undoubtibly slower, but the safety gain
	// is valuable.
	// I believe that we should adopt a perspective of all code being written as temporary,
	// which will benefit game design through rapid prototyping. These things seem small ("it's easy and simple,
	// I might as well optimize it"), but they add up, and complexity is exponential. Reducing complexity by 10%
	// can make it 3x faster to change the game design down the road.
	// We also get the benefit of actually looking at the performance difference between optimized an
	// unoptimized which often provides unexpected insights into the codebase as a whole.
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

		if (m_player == null)
		{
			Debug.LogWarning("[ActorManager] No reference to the player's transform was set. The system cannot work without " +
				"the reference for proximity calculations.", this);
		}
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
		m_playerPosition = m_player.transform.position;

		TickActors(Time.deltaTime);
	}

	public bool TryAddActor(Actor actor)
	{
		if (!s_Actors.Contains(actor))
		{
			s_Actors.Add(actor);

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
}
