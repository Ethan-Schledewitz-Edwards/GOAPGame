using System;
using System.Collections.Generic;
using UnityEngine;

public class ActorManager : MonoBehaviour
{
	private const int k_tps = 1;
	private const int k_initialActors = 12;// For debug

	[SerializeField] private Actor m_actorPrefab;

	private List<Actor> m_actors = new List<Actor>(k_initialActors);

	private int nextActorIndex = 0;

	private void Start()
	{
		int sqrt = Mathf.CeilToInt(Mathf.Sqrt(k_initialActors));

		for (int x = 0; x < sqrt; x++)
		{
			for (int z = 0; z < sqrt; z++)
			{
				if (m_actors.Count >= k_initialActors) 
					return;

				Actor actor = Instantiate(m_actorPrefab, null);
				actor.transform.position = new Vector3(x, 1, z);
				m_actors.Add(actor);
			}
		}
	}

	private void Update()
	{
		TickActors();
	}

	public void AddActor(Actor actor)
	{
		m_actors.Add(actor);
	}

	public void RemoveActor(Actor actor)
	{
		m_actors.Remove(actor);
	}

	private void TickActors()
	{
		if (m_actors.Count > 0) 
		{
			int actorsToTickThisFrame = Mathf.Max(1, Mathf.RoundToInt(m_actors.Count * Time.deltaTime));

			for (int i = 0; i < actorsToTickThisFrame; i++)
			{
				if (nextActorIndex >= m_actors.Count) nextActorIndex = 0;

				m_actors[nextActorIndex].TickBehaviour();
				nextActorIndex++;
			}
		}
	}
}
