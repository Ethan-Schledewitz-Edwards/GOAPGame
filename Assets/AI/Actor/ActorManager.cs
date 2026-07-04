using System;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.AI;
using SaveLoad.Management;

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

	double m_accumulatedTime = 0f;
	private Vector3 m_playerPosition;

	private void Awake()
	{
		if (Instance == null)
			Instance = this;
		else Destroy(Instance);
	}


	private void OnEnable()
	{
		SaveManager.GameLoaded += OnGameLoaded;
	}

	private void OnDestroy()
	{
		SaveManager.GameLoaded -= OnGameLoaded;
	}

	private void Update()
	{
		TickActors(Time.deltaTime);
	}

	private void OnGameLoaded(SaveManager.SaveData saveData)
	{
		SyncSimulation(saveData.SaveTime, System.DateTime.Now);
	}

	public void SyncSimulation(DateTime lastSave, DateTime now)
	{
		double offlineSeconds = (now - lastSave).TotalSeconds;
		double clampedSeconds = Math.Min(offlineSeconds, 86400); // 24h cap

		Debug.Log($"There were {offlineSeconds} between save and load");

		/*
		// Update World First
		foreach (var resource in allResources)
		{
			resource.FastForward(clampedSeconds);
		}

		// Update Actors
		foreach (var actor in allActors)
		{
			actor.ResolveOfflineTime(clampedSeconds);

			// After skipping, ensure they are on the NavMesh
			NavMeshHit hit;
			if (NavMesh.SamplePosition(actor.transform.position, out hit, 2.0f, NavMesh.AllAreas))
			{
				actor.transform.position = hit.position;
			}
		}
		*/
	}

	public void AddActor(Actor actor)
	{
		m_actors.Add(actor);
	}

	public void RemoveActor(Actor actor)
	{
		m_actors.Remove(actor);
	}

	private void TickActors(float t)
	{
		m_accumulatedTime += t;

		while (m_accumulatedTime >= k_tpsThreshold)
		{
			foreach (Actor actor in m_actors)
			{
				if (actor != null)
				{
					float distToPlayerSqrt = (m_playerPosition - actor.transform.position).sqrMagnitude;
					actor.Pathing.UpdateActorSimFidelity(distToPlayerSqrt);

					actor.TickBehaviour(k_tpsThreshold);
				}
			}

			m_accumulatedTime -= k_tpsThreshold;
		}
	}

	public void SetPlayerPosition(Vector3 playerPosition)
	{
		m_playerPosition = playerPosition;
	}
}
