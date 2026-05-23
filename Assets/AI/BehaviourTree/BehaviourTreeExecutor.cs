using BehaviourTrees;
using UnityEngine;

public class BehaviourTreeExecutor : MonoBehaviour
{
	[Header("Parameters")]
	public float InteractionDist { get; private set; } = 3.0f;

	public BehaviourTree BehaviourTree { get; private set; } = null;

	public void SetBehaviourTree(BehaviourTree behaviourTree)
	{
		BehaviourTree = behaviourTree;
	}
}
