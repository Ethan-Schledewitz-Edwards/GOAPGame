using AI.AIContext;
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

		protected override EBTNodeState OnNodeEvaluated(ref AIContext context, float t)
		{
			if (m_childNodes == null || m_childNodes.Count == 0)
				return EBTNodeState.SUCSESS;

			if (!context.SequenceIndices.TryGetValue(m_sequenceIndexKey, out int currentChildIndex))
			{
				currentChildIndex = 0;
			}

			while (currentChildIndex < m_childNodes.Count)
			{
				BTNodeBase currentChild = m_childNodes[currentChildIndex];
				EBTNodeState childState = currentChild.EvaluateNode(ref context, t);

				switch (childState)
				{
					case EBTNodeState.FAILURE:
						currentChild.ExitNode(ref context);
						context.SequenceIndices[m_sequenceIndexKey] = 0; // Reset for next execution cycle
						return EBTNodeState.FAILURE;

					case EBTNodeState.SUCSESS:
						currentChild.ExitNode(ref context);
						currentChildIndex++;
						context.SequenceIndices[m_sequenceIndexKey] = currentChildIndex; // Advance to the next task
						break;

					case EBTNodeState.RUNNING:
						return EBTNodeState.RUNNING; // Resume next frame
				}
			}

			context.SequenceIndices[m_sequenceIndexKey] = 0;
			return EBTNodeState.SUCSESS;
		}

		protected override void OnFirstEvaluate(ref AIContext context)
		{
			context.SequenceIndices[m_sequenceIndexKey] = 0;
		}

		protected override void OnNodeExited(ref AIContext context)
		{
			context.SequenceIndices.Remove(m_sequenceIndexKey);
		}

		protected override void OnNodeReset(ref AIContext context)
		{
			context.SequenceIndices.Remove(m_sequenceIndexKey);
		}
	}
}
