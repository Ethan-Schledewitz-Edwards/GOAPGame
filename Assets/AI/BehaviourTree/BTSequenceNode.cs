using System.Collections.Generic;

namespace BehaviourTrees
{
	public class BTSequenceNode : BTNodeBase
	{
        #region Constructors

        public BTSequenceNode() : base() { }
		public BTSequenceNode(List<BTNodeBase> children) : base(children) { }
        #endregion

        /// <summary>
        /// Returns success only if all children succeed, returning failure immediately if any child fails.
        /// </summary>
        public override EBTNodeState Evaluate()
		{
			bool isAnyChildRunning = false;

			foreach (BTNodeBase i in m_childNodes)
			{
				switch(i.Evaluate())
				{
					case EBTNodeState.STATE_FAILURE:
						m_nodeState = EBTNodeState.STATE_FAILURE;
						return m_nodeState;

					case EBTNodeState.STATE_SUCSESS:
						continue;

					case EBTNodeState.STATE_RUNNING:
						isAnyChildRunning = true;
						continue;
				}
			}

			m_nodeState = isAnyChildRunning ? EBTNodeState.STATE_RUNNING : EBTNodeState.STATE_SUCSESS;
			return m_nodeState;
		}
	}
}
