using UnityEngine;

namespace BehaviourTrees
{
	public class BehaviourTree
	{
		private BTNode m_rootNode;

		public BehaviourTree(BehaviourTree behaviourTree)
		{
			this.behaviourTree = behaviourTree;
		}

		public void TickBehaviourTree()
		{
			m_rootNode.Evaluate();
		}
	}
}
