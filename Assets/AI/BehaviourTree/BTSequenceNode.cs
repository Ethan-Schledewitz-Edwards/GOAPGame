using System.Collections.Generic;

namespace BehaviourTrees
{
	/// <summary>
	/// Returns success only if all children succeed, returning failure immediately if any child fails.
	/// </summary>
	public class BTSequenceNode : BTNodeBase
	{
		private readonly string m_sequenceIndexKey;

		public BTSequenceNode(List<BTNodeBase> children) : base(children) 
		{
			m_sequenceIndexKey = $"{NodeID}_SequenceIndex";
		}

		protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
		{
			if (m_childNodes == null || m_childNodes.Count == 0)
				return EBTNodeState.STATE_SUCSESS;

			int currentChildIndex = context.GetData<int>(m_sequenceIndexKey);

			while (currentChildIndex < m_childNodes.Count)
			{
				BTNodeBase currentChild = m_childNodes[currentChildIndex];
				EBTNodeState childState = currentChild.EvaluateNode(context, t);

				switch (childState)
				{
					case EBTNodeState.STATE_FAILURE:
						currentChild.ExitNode(context);
						context.SetData<int>(m_sequenceIndexKey, 0); // Reset for next execution cycle
						return EBTNodeState.STATE_FAILURE;

					case EBTNodeState.STATE_SUCSESS:
						currentChild.ExitNode(context);
						currentChildIndex++;
						context.SetData<int>(m_sequenceIndexKey, currentChildIndex); // Advance to the next task
						break;

					case EBTNodeState.STATE_RUNNING:
						return EBTNodeState.STATE_RUNNING; // Resume next frame
				}
			}

			context.SetData<int>(m_sequenceIndexKey, 0);
			return EBTNodeState.STATE_SUCSESS;
		}

		protected override void OnFirstEvaluate(AIContext context) 
		{
			context.SetData<int>(m_sequenceIndexKey, 0);
		}

		protected override void OnNodeExited(AIContext context) 
		{
			context.ClearData(m_sequenceIndexKey);
		}

		protected override void OnNodeReset(AIContext context)
		{
			context.ClearData(m_sequenceIndexKey);
		}
	}
}
