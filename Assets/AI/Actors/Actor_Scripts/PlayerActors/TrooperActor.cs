using AI.Context;
using Interaction.Core;
using UnityEngine;
using UnityEngine.AI;

namespace AI.Actors
{
	public class TrooperActor : PlayerActorBase, ICarryingInteractor
	{
		public void BeginCarrying(Transform parent, Vector3 localPosition)
		{
			if (parent != null)
			{
				SetLogicExecutorState(EPlayerActorState.Carrying);

				AIPathing.NavAgent.enabled = false;
				AIPathing.ClearDestination();

				Collider.isTrigger = true;

				transform.parent = parent;
				transform.localPosition = localPosition;
			}
		}

		public void EndCarrying()
		{
			// Detach from the carryable
			transform.SetParent(null, true);

			// Restore the collider
			if (Collider != null)
				Collider.isTrigger = false;

			// Re-enable navigation
			if (AIPathing.NavAgent != null)
			{
				AIPathing.NavAgent.enabled = true;

				// Find a valid NavMesh position near the actor
				if (NavMesh.SamplePosition(
						transform.position,
						out NavMeshHit hit,
						3f,
						AIPathing.NavAgent.areaMask))
				{
					AIPathing.NavAgent.Warp(hit.position);
					AIPathing.NavAgent.isStopped = false;
				}
			}
		}
	}
}
