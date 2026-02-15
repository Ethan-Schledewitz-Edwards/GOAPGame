using UnityEngine;

public class ActorManager : MonoBehaviour
{
	private const int k_tps = 1;

	[SerializeField] private Actor[] Actors;

	private int nextActorIndex = 0;

	private void Update()
	{
		TickActors();
	}

	private void TickActors()
	{
		if (Actors.Length > 0) 
		{
			int actorsToTickThisFrame = Mathf.Max(1, Mathf.RoundToInt(Actors.Length * Time.deltaTime));

			for (int i = 0; i < actorsToTickThisFrame; i++)
			{
				if (nextActorIndex >= Actors.Length) nextActorIndex = 0;

				Actors[nextActorIndex].TickBehaviour();
				nextActorIndex++;
			}
		}
	}
}
