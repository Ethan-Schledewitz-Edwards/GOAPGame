namespace BehaviourTrees
{
	public class BehaviourTree
	{
		private BTNodeBase m_rootNode;

		public void TickBehaviourTree()
		{
			m_rootNode.Evaluate();
		}

		public void SetTree(BTNodeBase rootNode)
		{
			m_rootNode = rootNode;
		}
	}
}
