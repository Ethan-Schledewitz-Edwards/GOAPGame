using System;
using System.Collections.Generic;
using UnityEditor.Overlays;
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

	private const int k_initialActors = 12;// For debug
	#endregion

	[SerializeField] private Actor m_actorPrefab;

	private List<Actor> m_actors = new List<Actor>(k_initialActors);

	double m_accumulatedTime = 0f;
	private Vector3 m_playerPosition;

	private void Awake()
	{
		if (Instance == null)
			Instance = this;
		else Destroy(Instance);
	}

	private void Start()
	{
		int sqrt = Mathf.CeilToInt(Mathf.Sqrt(k_initialActors));

		int nextActorIndex = 0;
		for (int x = 0; x < sqrt; x++)
		{
			for (int z = 0; z < sqrt; z++)
			{
				if (m_actors.Count >= k_initialActors) 
					return;

				Actor actor = Instantiate(m_actorPrefab, null);
				actor.name = $"{m_actorPrefab.name}: {nextActorIndex}";
				actor.transform.position = new Vector3(x, 5, z);
				AddActor(actor);

				nextActorIndex++;
			}
		}
	}

	private void OnEnable()
	{
		SaveManager.Instance.OnGameLoaded += OnGameLoaded;
	}

	private void OnDisable()
	{
		SaveManager.Instance.OnGameLoaded -= OnGameLoaded;
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
					actor.AIPathing.UpdateActorSimFidelity(distToPlayerSqrt);

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
