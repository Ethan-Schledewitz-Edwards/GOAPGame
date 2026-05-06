namespace BehaviourTrees
{
	public class BehaviourTree
	{
		private BTNodeBase m_rootNode;

		public void TickBehaviourTree(float t)
		{
			m_rootNode.Evaluate(t);
		}

		public void SetTree(BTNodeBase rootNode)
		{
			m_rootNode = rootNode;
		}

		/// <summary>
		/// Checks the tree's root's context data dictionary for a key
		/// </summary>
		/// <param name="key">The key pointing to the data</param>
		/// <param name="value">A pointer to the data</param>
		/// <returns>If the desired data was found</returns>
		public bool TryGetData(string key, out object value)
		{
			value = m_rootNode.GetData(key);

			return value != null;
		}
	}
}
