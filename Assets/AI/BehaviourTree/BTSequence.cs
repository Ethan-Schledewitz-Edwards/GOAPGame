using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTrees
{
	public class BTSequence : BTNodeBase
	{
		public BTSequence() : base() { }
		public BTSequence(List<BTNodeBase> children) : base(children) { }

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
					default:
						m_nodeState = EBTNodeState.STATE_SUCSESS;
						return m_nodeState;

				}
			}

			m_nodeState = isAnyChildRunning ? EBTNodeState.STATE_RUNNING : EBTNodeState.STATE_SUCSESS;
			return m_nodeState;
		}

	}
}
