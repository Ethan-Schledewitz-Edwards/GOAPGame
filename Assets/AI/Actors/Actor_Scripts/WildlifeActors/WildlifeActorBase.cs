using BehaviourTrees;
using UnityEngine;

namespace AI.Actors
{
	public abstract class WildlifeActorBase : ActorBase
	{
		protected override void Awake()
		{
			base.Awake();

			//if (m_behaviourTree == null)
			//{
			//	Debug.LogWarning(
			//		$"{name} has no wildlife behaviour tree assigned.",
			//		this
			//	);

			//	return;
			//}

			//SetBehaviourTree(m_behaviourTree);
		}
	}
}