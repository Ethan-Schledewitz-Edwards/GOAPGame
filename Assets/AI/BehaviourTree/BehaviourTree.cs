using UnityEngine;

namespace BehaviourTrees
{
	public class BehaviourTree
	{
		private BTNode m_rootNode;

		public void TickBehaviourTree()
		{
			m_rootNode.Evaluate();
		}

		public void SetTree(BTNode rootNode)
		{
			m_rootNode = rootNode;
		}
	}
}
