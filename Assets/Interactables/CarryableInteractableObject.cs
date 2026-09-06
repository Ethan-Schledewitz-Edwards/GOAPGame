using BehaviourTrees;
using UnityEngine;

public class CarryableInteractableObject : InteractableObjectBase
{
	private float m_moveSpeed = 0f;

	public override void UpdateSpeed(int extra)
	{
		m_moveSpeed = 2f + (extra * 1.5f);
	}

	public override void StopInteractSpeed()
	{
		m_moveSpeed = 0f;
	}

	public override BehaviourTree GetBehaviourTree() => null;
}
