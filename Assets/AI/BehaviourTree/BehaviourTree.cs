namespace BehaviourTrees
{
	public class BehaviourTree
	{
		private BTNodeBase m_rootNode;

		public void TickBehaviourTree(AIContext aiContext, float t)
		{
			m_rootNode.Evaluate(aiContext, t);
		}

		public void SetTree(BTNodeBase rootNode)
		{
			m_rootNode = rootNode;
		}
	}
}
