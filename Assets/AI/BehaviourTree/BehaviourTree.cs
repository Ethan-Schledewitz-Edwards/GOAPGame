using System;
using UnityEngine;

namespace BehaviourTrees
{
	public class BehaviourTree
	{
		private BTNodeBase m_rootNode;

		public EBTNodeState TickBehaviourTree(AIContext aiContext, float t)
		{
			if (m_rootNode != null)
			{
				return m_rootNode.EvaluateNode(aiContext, t);
			}
			else
			{
				Debug.LogWarning("A behaviour tree is attempting to execute without a root node");
				return EBTNodeState.STATE_FAILURE;
			}
		}

		public void SetTree(BTNodeBase rootNode)
		{
			m_rootNode = rootNode;
		}
	}
}
