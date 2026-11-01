using AI.Context;
using UnityEngine;

namespace AI.Actors
{
	public class TrooperActor : PlayerActorBase
	{
		protected override void HandleContextRest()
		{
			AIContext context = BehaviourTreeExecutor.AIContext;

			context.ExecutorTransform = transform;
			context.ExecutorFaction = (int)ActorFaction;
			context.JobSearchDistance = ActorBase.c_JobSearchRange;
			context.InteractionLayer = 1 << LayerMask.NameToLayer("Interaction");
			context.AttackDamage = 1; // Hard coded for now
		}
	}
}
