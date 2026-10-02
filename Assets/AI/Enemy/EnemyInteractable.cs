using BehaviourTrees;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EnemyInteractable : ActorInteractableBase
{
	private static BehaviourTree s_AttackBehaviour;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	static void Init()
	{
		s_AttackBehaviour = null;
	}

	void Awake()
	{
		if (s_AttackBehaviour == null)
		{
			BTSequenceNode attackSequence = new(new List<BTNodeBase>()
			{
				new MoveToInteractionPositionTask(),
				new BTTimeoutNode(new CheckForDestinationRangeTask(), 2f),
				new AttackTask(),
				new BTTimeoutNode(new SearchForClosestJobTask(), 2f),
				new BTTimeoutNode(new ReserveInteractionPositionTask(), 2f),
				new MoveToInteractionPositionTask(),
				new BTTimeoutNode(new CheckForDestinationRangeTask(), 2f),
				new BTTimeoutNode(new AquireNewBehaviourTreeFromTargetTask(), 2f)
			});

			s_AttackBehaviour = new();
			s_AttackBehaviour.SetTree(attackSequence);
		}
	}

	public override BehaviourTree GetBehaviourTree()
	{
		return s_AttackBehaviour;
	}
}
