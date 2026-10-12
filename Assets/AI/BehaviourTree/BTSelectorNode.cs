using AI.AIContext;
using System.Collections.Generic;

namespace BehaviourTrees
{
	/// <summary>
	/// Evaluates children in order and returns the state of the first one that does not fail.
	/// </summary>
	public class BTSelectorNode : BTNodeBase
	{
		public BTSelectorNode() : base() { }
		public BTSelectorNode(List<BTNodeBase> children) : base(children) { }

		protected override EBTNodeState OnNodeEvaluated(ref AIContext context, float t)
		{
			foreach (BTNodeBase i in m_childNodes)
			{
				switch (i.EvaluateNode(ref context, t))
				{
					case EBTNodeState.FAILURE:
						continue;

					case EBTNodeState.SUCSESS:
						return EBTNodeState.SUCSESS;

					case EBTNodeState.RUNNING:
						return EBTNodeState.RUNNING;
				}
			}

			return EBTNodeState.FAILURE;
		}

		protected override void OnFirstEvaluate(ref AIContext context) { }

		protected override void OnNodeExited(ref AIContext context) { }

		protected override void OnNodeReset(ref AIContext context) { }
	}
}