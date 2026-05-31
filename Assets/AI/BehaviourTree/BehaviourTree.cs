using System;
using UnityEngine;

namespace BehaviourTrees
{
	public class BehaviourTree
	{
		private BTNodeBase m_rootNode;

		public void TickBehaviourTree(AIContext aiContext, float t)
		{
			if (m_rootNode != null)
			{
				m_rootNode.Evaluate(aiContext, t);
			}
			else
			{
				Debug.LogWarning("A behaviour tree is attempting to execute without a root node");
			}
		}

		public void SetTree(BTNodeBase rootNode)
		{
			m_rootNode = rootNode;
		}
	}
}
