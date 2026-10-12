using AI.AIContext;
using System;
using UnityEngine;

namespace BehaviourTrees
{
	public class BehaviourTree
	{
		private BTNodeBase m_rootNode;

		public EBTNodeState TickBehaviourTree(ref AIContext aiContext, float t)
		{
			if (m_rootNode != null)
			{
				EBTNodeState state = m_rootNode.EvaluateNode(ref aiContext, t);

				if (state != EBTNodeState.RUNNING)
				{
					m_rootNode.ExitNode(ref aiContext);
				}

				return state;
			}
			else
			{
				Debug.LogWarning("A behaviour tree is attempting to execute without a root node");
				return EBTNodeState.FAILURE;
			}
		}

		public void SetTree(BTNodeBase rootNode)
		{
			m_rootNode = rootNode;
		}
	}
}
