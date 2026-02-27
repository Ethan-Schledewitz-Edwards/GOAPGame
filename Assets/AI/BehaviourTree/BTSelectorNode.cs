using System.Collections.Generic;

namespace BehaviourTrees
{
	public class BTSelectorNode : BTNodeBase
	{
        #region Constructors

        public BTSelectorNode() : base() { }
		public BTSelectorNode(List<BTNodeBase> children) : base(children) { }
        #endregion

        /// <summary>
        /// Evaluates children in order and returns if one has not failed.
        /// </summary>
        public override EBTNodeState Evaluate()
		{
			foreach (BTNodeBase i in m_childNodes)
			{
				switch (i.Evaluate())
				{
					case EBTNodeState.STATE_FAILURE:
						continue;

					case EBTNodeState.STATE_SUCSESS:
						m_nodeState = EBTNodeState.STATE_SUCSESS; 
						return m_nodeState;

					case EBTNodeState.STATE_RUNNING:
						m_nodeState = EBTNodeState.STATE_RUNNING;
						return m_nodeState;

				}
			}

			m_nodeState = EBTNodeState.STATE_FAILURE;
			return m_nodeState;
		}
	}
}