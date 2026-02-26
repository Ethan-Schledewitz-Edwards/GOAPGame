using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTrees
{
	public class BTSelector : BTNodeBase
	{
		public BTSelector() : base() { }
		public BTSelector(List<BTNodeBase> children) : base(children) { }

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
					default:
						continue;

				}
			}

			m_nodeState = EBTNodeState.STATE_FAILURE;
			return m_nodeState;
		}

	}
}