using System.Collections.Generic;

namespace BehaviourTrees
{
	/// <summary>
	/// Returns success only if all children succeed, returning failure immediately if any child fails.
	/// </summary>
	public class BTSequenceNode : BTNodeBase
	{
		public BTSequenceNode() : base() { }
		public BTSequenceNode(List<BTNodeBase> children) : base(children) { }

		protected override EBTNodeState OnUpdate(AIContext context, float t)
		{
			bool isAnyChildRunning = false;

			foreach (BTNodeBase i in m_childNodes)
			{
				switch (i.Evaluate(context, t))
				{
					case EBTNodeState.STATE_FAILURE:
						return EBTNodeState.STATE_FAILURE;

					case EBTNodeState.STATE_SUCSESS:
						continue;

					case EBTNodeState.STATE_RUNNING:
						isAnyChildRunning = true;
						continue;
				}
			}

			return isAnyChildRunning ? EBTNodeState.STATE_RUNNING : EBTNodeState.STATE_SUCSESS;
		}

		protected override void OnFirstEvaluate(AIContext context) { }
	}
}
