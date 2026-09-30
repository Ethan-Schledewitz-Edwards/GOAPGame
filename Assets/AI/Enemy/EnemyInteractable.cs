using BehaviourTrees;
using UnityEditor;
using UnityEngine;

public class EnemyInteractable : InteractableObjectBase
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
			//BTSequenceNode attackSequence = new();

			s_AttackBehaviour = new();
			//s_AttackBehaviour.SetTree(attackSequence);
		}
	}

	public override BehaviourTree GetBehaviourTree()
	{
		return null;
	}
}
